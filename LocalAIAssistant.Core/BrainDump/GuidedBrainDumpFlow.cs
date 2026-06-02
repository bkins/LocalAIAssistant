using System.Text;

namespace LocalAIAssistant.Core.BrainDump;

public class GuidedBrainDumpFlow : IGuidedBrainDumpFlow
{
    private readonly IBrainDumpApiClient _client;

    private FlowState? _state;

    public bool IsActive => _state is not null;

    public GuidedBrainDumpFlow(IBrainDumpApiClient client)
    {
        _client = client;
    }

    // ── Trigger detection ─────────────────────────────────────────────────────

    private static readonly string[] Triggers =
    {
        "brain dump"
      , "braindump"
      , "guided brain dump"
      , "guided braindump"
      , "let's do a brain dump"
      , "let's brain dump"
      , "start a brain dump"
      , "start brain dump"
      , "guided journal"
      , "help me unload my thoughts"
      , "help me unload"
      , "let me brain dump"
    };

    public bool IsTrigger(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return Triggers.Any(trigger => normalized == trigger
                                    || normalized.StartsWith(trigger + " ", StringComparison.Ordinal)
                                    || normalized.StartsWith(trigger + ",", StringComparison.Ordinal));
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    public async Task<FlowTurn> StartAsync( Func<string, CancellationToken, Task<string>> converseFn
                                          , CancellationToken                             ct = default )
    {
        var session = await _client.StartSessionAsync(ct);

        _state = new FlowState
                 {
                     SessionId     = session.Id
                   , CategoryIndex = 0
                   , Phase         = FlowPhase.CollectingCategories
                   , ConverseFn    = converseFn
                 };

        var intro = "Let's do a guided brain dump. Work through each category at your own pace.\n"
                  + "Type \"skip\" to skip a section, or \"done\" to finish early.\n\n";

        return new FlowTurn(intro + BrainDumpCategories.All[0].Prompt);
    }

    // ── Main input router ─────────────────────────────────────────────────────

    public async Task<FlowTurn> HandleInputAsync(string input, CancellationToken ct = default)
    {
        if (_state is null)
            return new FlowTurn("No active brain dump session.", FlowAction.Done);

        if (IsCancelCommand(input))
        {
            Reset();
            return new FlowTurn("Brain dump cancelled. Your session has been discarded.", FlowAction.Done);
        }

        return _state.Phase switch
        {
            FlowPhase.CollectingCategories => await HandleCategoryAsync(input, ct)
          , FlowPhase.ConfirmingItems      => await HandleConfirmationAsync(input, ct)
          , _                              => new FlowTurn("Brain dump complete.", FlowAction.Done)
        };
    }

    // ── Category collection phase ─────────────────────────────────────────────

    private async Task<FlowTurn> HandleCategoryAsync(string input, CancellationToken ct)
    {
        var category = BrainDumpCategories.All[_state!.CategoryIndex];
        var isSkip   = IsSkipCommand(input);
        var isDone   = IsDoneCommand(input);

        if (!isSkip && !isDone)
        {
            await _client.UpdateCategoryAsync(_state.SessionId, category.Field, input.Trim(), ct);
        }

        _state.CategoryIndex++;

        var allCategoriesDone = isDone || _state.CategoryIndex >= BrainDumpCategories.All.Count;

        if (allCategoriesDone)
            return await RunExtractionAsync(ct);

        var skippedNote = isSkip ? "*(skipped)*\n\n" : string.Empty;
        var nextPrompt  = BrainDumpCategories.All[_state.CategoryIndex].Prompt;

        return new FlowTurn(skippedNote + nextPrompt);
    }

    // ── Extraction ────────────────────────────────────────────────────────────

    private async Task<FlowTurn> RunExtractionAsync(CancellationToken ct)
    {
        try
        {
            var session = await _client.GetSessionAsync(_state!.SessionId, ct);
            var prompt  = BuildExtractionPrompt(session ?? new BrainDumpSessionDto());
            var llmResponse = await _state.ConverseFn!(prompt, ct);

            var items = ParseExtractedItems(llmResponse);

            if (items.Count == 0)
            {
                await _client.MarkProcessedAsync(_state.SessionId, null, [], ct);
                Reset();
                return new FlowTurn(
                    "Your brain dump has been saved. I didn't find clear action items to review, "
                  + "but everything you wrote is captured. Say \"show my last brain dump\" to see it."
                  , FlowAction.Done
                );
            }

            _state.PendingItems = items;
            _state.ItemIndex    = 0;
            _state.Phase        = FlowPhase.ConfirmingItems;

            return new FlowTurn(
                $"Brain dump captured! I found **{items.Count}** item(s) to review.\n\n"
              + FormatCurrentItem()
            );
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            await _client.MarkProcessedAsync(_state!.SessionId, null, [], ct);
            Reset();
            return new FlowTurn(
                "Your brain dump has been saved. (Analysis hit a snag — you can review it later with \"show my last brain dump\".)"
              , FlowAction.Done
            );
        }
    }

    private static string BuildExtractionPrompt(BrainDumpSessionDto session)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Extract actionable items from this brain dump.");
        sb.AppendLine("Return ONLY a list — one item per line in this exact format, no other text:");
        sb.AppendLine("TASK: description of something to do");
        sb.AppendLine("CONCERN: description of something to process or be aware of");
        sb.AppendLine("PATTERN: a recurring theme across the categories");
        sb.AppendLine();
        sb.AppendLine("Brain dump content:");

        AppendCategory(sb, "Avoidance",         session.Avoidance);
        AppendCategory(sb, "Fears",             session.Fears);
        AppendCategory(sb, "Frustrations",      session.Frustrations);
        AppendCategory(sb, "Discouragements",   session.Discouragements);
        AppendCategory(sb, "Goals & Barriers",  session.GoalsAndBarriers);
        AppendCategory(sb, "Hurt & Sorrow",     session.HurtAndSorrow);
        AppendCategory(sb, "Self-Criticism",    session.SelfCriticism);

        return sb.ToString().TrimEnd();
    }

    private static void AppendCategory(StringBuilder sb, string label, string? text)
    {
        var value = string.IsNullOrWhiteSpace(text) ? "(skipped)" : text.Trim();
        sb.AppendLine($"{label}: {value}");
    }

    public static IReadOnlyList<ExtractedItem> ParseExtractedItems(string llmResponse)
    {
        var items = new List<ExtractedItem>();

        foreach (var rawLine in llmResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("TASK:", StringComparison.OrdinalIgnoreCase))
            {
                var desc = line[5..].Trim();
                if (desc.Length > 0)
                    items.Add(new ExtractedItem(ExtractedItemType.Task, desc));
            }
            else if (line.StartsWith("CONCERN:", StringComparison.OrdinalIgnoreCase))
            {
                var desc = line[8..].Trim();
                if (desc.Length > 0)
                    items.Add(new ExtractedItem(ExtractedItemType.Concern, desc));
            }
            else if (line.StartsWith("PATTERN:", StringComparison.OrdinalIgnoreCase))
            {
                var desc = line[8..].Trim();
                if (desc.Length > 0)
                    items.Add(new ExtractedItem(ExtractedItemType.Pattern, desc));
            }
        }

        return items;
    }

    // ── Confirmation phase ────────────────────────────────────────────────────

    private async Task<FlowTurn> HandleConfirmationAsync(string input, CancellationToken ct)
    {
        var normalized   = input.Trim().ToLowerInvariant();
        var currentItem  = _state!.PendingItems[_state.ItemIndex];
        var isYes        = normalized is "yes" or "y" or "add" or "add it" or "yep" or "yeah";
        var isSkipAll    = normalized.StartsWith("skip rest", StringComparison.Ordinal)
                        || normalized.StartsWith("skip all",  StringComparison.Ordinal)
                        || normalized is "done" or "finish" or "stop";

        if (isSkipAll)
        {
            var summary = BuildSummary();
            await FinishAsync(ct);
            return new FlowTurn(summary, FlowAction.Done);
        }

        _state.ItemIndex++;

        if (isYes && currentItem.Type == ExtractedItemType.Task)
        {
            _state.ConfirmedTitles.Add(currentItem.Description);

            if (_state.ItemIndex >= _state.PendingItems.Count)
            {
                var summary = BuildSummary();
                await FinishAsync(ct);
                return new FlowTurn(summary, FlowAction.CreateTask, currentItem.Description);
            }

            return new FlowTurn(
                $"✓ Queued: *{currentItem.Description}*\n\n" + FormatCurrentItem()
              , FlowAction.CreateTask
              , currentItem.Description
            );
        }

        if (isYes && currentItem.Type != ExtractedItemType.Task)
        {
            // Concerns and patterns are noted but not added as tasks
            _state.ConfirmedTitles.Add(currentItem.Description);
        }

        if (_state.ItemIndex >= _state.PendingItems.Count)
        {
            var summary = BuildSummary();
            await FinishAsync(ct);
            return new FlowTurn(summary, FlowAction.Done);
        }

        var skippedNote = isYes ? string.Empty : "*(skipped)*\n\n";
        return new FlowTurn(skippedNote + FormatCurrentItem());
    }

    private async Task FinishAsync(CancellationToken ct)
    {
        var summary = $"Captured {_state!.ConfirmedTitles.Count} item(s) on {DateTimeOffset.Now:MMM d, yyyy}";
        await _client.MarkProcessedAsync(_state.SessionId, summary, _state.ConfirmedTitles, ct);
        Reset();
    }

    private string BuildSummary()
    {
        var confirmed = _state!.ConfirmedTitles;
        if (confirmed.Count == 0)
            return "Brain dump saved. No items were added — you can review it with \"show my last brain dump\".";

        var sb = new StringBuilder();
        sb.AppendLine($"Brain dump complete! **{confirmed.Count}** item(s) captured:");
        sb.AppendLine();

        foreach (var title in confirmed)
            sb.AppendLine($"• {title}");

        sb.AppendLine();
        sb.Append("Tasks have been queued. Say \"show my tasks\" to see them.");

        return sb.ToString().TrimEnd();
    }

    private string FormatCurrentItem()
    {
        var item  = _state!.PendingItems[_state.ItemIndex];
        var label = item.Type switch
                    {
                        ExtractedItemType.Task    => "potential task"
                      , ExtractedItemType.Concern => "concern to note"
                      , ExtractedItemType.Pattern => "recurring pattern"
                      , _                         => "item"
                    };

        var total    = _state.PendingItems.Count;
        var position = _state.ItemIndex + 1;

        return $"**{position} of {total}** — {label}: *\"{item.Description}\"*\n\nAdd it? (yes / no / skip rest)";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public void Reset()
    {
        _state = null;
    }

    private static bool IsSkipCommand(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized is "skip" or "s" or "next" or "pass";
    }

    private static bool IsDoneCommand(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized is "done" or "finish" or "that's it" or "thats it" or "enough";
    }

    private static bool IsCancelCommand(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        return normalized is "cancel" or "abort" or "stop" or "quit" or "exit";
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private enum FlowPhase
    {
        CollectingCategories
      , ConfirmingItems
    }

    private sealed class FlowState
    {
        public required string   SessionId      { get; set; }
        public          int      CategoryIndex  { get; set; }
        public          int      ItemIndex      { get; set; }
        public          FlowPhase Phase         { get; set; }

        public IReadOnlyList<ExtractedItem> PendingItems    { get; set; } = [];
        public List<string>                 ConfirmedTitles { get; set; } = [];

        public Func<string, CancellationToken, Task<string>>? ConverseFn { get; set; }
    }
}
