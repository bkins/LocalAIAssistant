using System.Text.RegularExpressions;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.Services.AiMemory;

public class ConversationMemory : IConversationMemory
{

    private readonly IShortTermMemoryStore  _shortTermMemoryStore; // SQLIte
    private readonly ILongTermMemoryStore   _longTermMemoryStore;  // JSONL
    private readonly MemoryRetrievalOptions _policy;

    private readonly List<Message> _currentConversationMemory = new();
    private          bool          _initialized;

    public ConversationMemory(IShortTermMemoryStore            shortTemMemoryStore
                            , ILongTermMemoryStore             longTermMemoryStore
                            , IOptions<MemoryRetrievalOptions> policy)
    {
        _shortTermMemoryStore = shortTemMemoryStore ?? throw new ArgumentNullException(nameof(shortTemMemoryStore));
        _longTermMemoryStore  = longTermMemoryStore ?? throw new ArgumentNullException(nameof(longTermMemoryStore));
        _policy               = policy.Value ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task InitializeAsync()
    {
        // 1. This does not seem to be right.  Should I be adding the short term memory to the current conversation?
        // 2. Should I load the short term memory into a public property instead?
        // 3. If so, should I load the long term memory as well?
        var persistedMessages = await _shortTermMemoryStore.LoadMessagesAsync();
        
        _currentConversationMemory.Clear();
        _currentConversationMemory.AddRange(persistedMessages);
    }
    
    private static int EvaluateImportance(Message message)
    {
        var score = 1; // baseline
        var text = message.Content.ToLowerInvariant();

        // “things to remember”
        if (Regex.IsMatch(text, @"\b(remember|don't forget|do not forget)\b")) score += 2;
        
        // actionable / tasks
        if (Regex.IsMatch(text, @"\b(todo|task|action item|deadline|due|remind)\b")) score += 2;

        // preferences / identity
        if (Regex.IsMatch(text, @"\b(i like|i prefer|my favorite|call me|my name is)\b")) score += 2;

        // critical info
        if (Regex.IsMatch(text, @"\b(allergic|i'm allergic|emergency|medication)\b")) score += 3;

        // tags boost
        if (Regex.IsMatch(text, @"\b(summary|promotion)\b")) score += 1;
        
        // clamp to 1..5
        return Math.Max(1, Math.Min(5, score));
    }
    
    public async Task AddAsync(Message message)
    {
        await EnsureInitializedAsync();
        
        ArgumentNullException.ThrowIfNull(message);

        if (message.Importance <= 0) message.Importance = EvaluateImportance(message);

        _currentConversationMemory.Add(message);

        // Always persist to STM quickly; LTM only via promotion or explicit calls
        await _shortTermMemoryStore.SaveMessagesAsync(new[] { message });

        await TryPromoteAndTrimAsync();
    }
    
    private async Task TryPromoteAndTrimAsync()
    {
        if (_currentConversationMemory.Count <= _policy.MaxStmMessages) return;

        // Oldest block we’ll summarize/promote
        var overflow = _currentConversationMemory.Count - _policy.MaxStmMessages;
        var batch = Math.Max(overflow, _policy.PromotionBatchSize);
        var toPromote = _currentConversationMemory.OrderBy(message => message.Timestamp)
                                                  .Take(batch)
                                                  .ToList();

        if (toPromote.Count == 0) return;

        // 2a) Autopromote high-importance messages directly
        var highImportance = toPromote.Where(message => message.Importance >= 4).ToList();
        if (highImportance.Count > 0)
            await _longTermMemoryStore.SaveMessagesAsync(highImportance);

        
        // Build a compact summary
        var summary = _policy.SummarizeOnPromotion
                              ? SimpleCompressor.BuildSummary(toPromote
                                                            , []
                                                            , _policy.SummaryMaxChars
                                                            , _policy.IncludeTimestamps)
                              : string.Join("\n", toPromote.Select(message => $"{message.Sender}: {message.Content}"));

        var summaryMsg = new Message
                         {
                             Sender         = Senders.Memory
                           , Content        = summary
                           , Timestamp      = DateTime.UtcNow
                           , ConversationId = toPromote.FirstOrDefault()?.ConversationId ?? string.Empty
                           , Tags           = new() { "summary", "promotion" }
                           , Importance     = 2
                         };

        // Persist summary to LTM
        await _longTermMemoryStore.SaveMessageAsync(summaryMsg);

        // Trim STM (both DB and in-memory)
        var cutoff = toPromote.Last().Timestamp;

        await _shortTermMemoryStore.DeleteMessagesOlderThanAsync(cutoff);

        _currentConversationMemory.RemoveAll(message => message.Timestamp <= cutoff);
    }

    // public async Task AddAsync(Message message)
    // {
    //     if (message == null) throw new ArgumentNullException(nameof(message));
    //
    //     _currentConversationMemory.Add(message);
    //
    //     // Always write to short-term (for fast recall) and long-term (for permanent history)
    //     await _shortTermMemoryStore.SaveMessagesAsync(new[] { message });
    //     await _longTermMemoryStore.SaveMessagesAsync(new[] { message });
    // }

    public async Task<IEnumerable<Message>> GetRecentEntries(int count)
    {
        await EnsureInitializedAsync();
         
        return count <= 0
                       ? Enumerable.Empty<Message>()
                       : _currentConversationMemory.TakeLast(count);
    }

    public async Task<IEnumerable<Message>> GetEntriesSince(DateTime since)
    {
        //return await _currentConversationMemory.Where(m => m.Timestamp >= since);
        throw new NotImplementedException();
    }

    public async Task SaveAsync()
    {
        await EnsureInitializedAsync();
        await _shortTermMemoryStore.SaveMessagesAsync(_currentConversationMemory);
        await _longTermMemoryStore.SaveMessagesAsync(_currentConversationMemory);
    }

    public async Task ClearAsync()
    {
        // Just clears in-memory session history
        _currentConversationMemory.Clear();
        
        await EnsureInitializedAsync();
        await  Task.CompletedTask;
    }

    public Task<IEnumerable<Message>> LoadShortTermAsync() => _shortTermMemoryStore.LoadMessagesAsync();

    public async Task<IEnumerable<Message>> LoadLongTermAsync()
    {
        await EnsureInitializedAsync();
        return await  _longTermMemoryStore.LoadMessagesAsync();
    }

    public async Task ClearLongTermAsync()
    {
        await EnsureInitializedAsync();
        await _longTermMemoryStore.ClearMemoryAsync();
    }


    public async Task ClearShortTermAsync()
    {
        await EnsureInitializedAsync();
        await _shortTermMemoryStore.ClearMemoryAsync();
    }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await InitializeAsync();
        _initialized = true;
    }

}
