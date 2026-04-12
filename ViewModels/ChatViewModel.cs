using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private const string SelectedPersonalityPrefKey = StringConsts.SelectedPersonalityPrefKey;

    private readonly ILlmService                      _llmService;
    private readonly IConversationMemory              _conversationMemory;
    private readonly IMemoryService                   _memory;
    private readonly IPersonalityService              _personalityService;
    private readonly ILoggingService                  _log;
    private readonly IOptions<MemoryRetrievalOptions> _memoryRetrievalOptions;
    private readonly IOrchestratorService             _orchestrator;
    private readonly ICognitivePlatformClientFactory  _cpFactory;
    private readonly IConnectivityState               _apiState;
    private readonly IOfflineQueueService             _offlineQueueService;
    private readonly ApiHealthService                 _apiHealthService;
    private readonly AppShellMasterViewModel          _appShellMasterViewModel;

    // ── Connectivity ──────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isOffline;

    // ── UI state ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool        _useStreaming      = false;
    [ObservableProperty] private string      _promptText        = string.Empty;
    [ObservableProperty] private bool        _isBusy;
    [ObservableProperty] private bool        _isTyping;
    [ObservableProperty] private Personality _selectedPersonality;
    [ObservableProperty] private int         _pendingQueueCount;
    [ObservableProperty] private bool        _showStreamingOption = false;

    public string SuggestedModelToUse { get; set; } = "phi-3:mini";

    public ObservableCollection<Message>     Messages      { get; } = new();
    public ObservableCollection<Personality> Personalities { get; } = new();

    public string ConversationId { get; } = Guid.NewGuid().ToString();

    public ChatViewModel( ILlmService                      llmService
                        , IConversationMemory              conversationMemory
                        , IMemoryService                   memory
                        , IPersonalityService              personalityService
                        , ILoggingService                  log
                        , IOptions<MemoryRetrievalOptions> memoryOptions
                        , IOrchestratorService             orchestrator
                        , ICognitivePlatformClientFactory  cpFactory
                        , IConnectivityState               apiState
                        , IOfflineQueueService             offlineQueueService
                        , ApiHealthService                 apiHealthService
                        , AppShellMasterViewModel          appShellMasterViewModel )
    {
        _llmService              = llmService;
        _conversationMemory      = conversationMemory;
        _memory                  = memory;
        _personalityService      = personalityService;
        _log                     = log;
        _memoryRetrievalOptions  = memoryOptions;
        _orchestrator            = orchestrator;
        _cpFactory               = cpFactory;
        _apiState                = apiState;
        _offlineQueueService     = offlineQueueService;
        _apiHealthService        = apiHealthService;
        _appShellMasterViewModel = appShellMasterViewModel;

        _apiState.ConnectivityChanged += (_, _) => IsOffline = _apiState.IsOffline;
    }

    public async Task InitializeAsync()
    {
        var stm = await _conversationMemory.LoadShortTermAsync();

        Messages.Clear();

        foreach (var message in stm.OrderBy(message => message.Timestamp))
            Messages.Add(message);

        Personalities.Clear();

        var allPersonalities = _personalityService.GetAll();
        if (allPersonalities is { Count: > 0 })
        {
            foreach (var personality in allPersonalities)
                Personalities.Add(personality);
        }
        else
        {
            Personalities.Add(new Personality
                              {
                                      Name         = "The Barista"
                                    , SystemPrompt = "You are Jenny, a friendly coffee barista who remembers regulars."
                                    , Description  = "Warm, upbeat coffee expert."
                                    , IsDefault    = true
                              });
        }

        var lastPersonalityName = Preferences.Get(SelectedPersonalityPrefKey, Personalities.First().Name);
        SelectedPersonality = Personalities.FirstOrDefault(personality => personality.Name == lastPersonalityName)
                           ?? Personalities.First();
        _personalityService.SetCurrent(SelectedPersonality.Name);

        await _offlineQueueService.ResetProcessingItemsAsync();
        await _appShellMasterViewModel.RefreshQueueCountAsync();
    }

    partial void OnSelectedPersonalityChanged(Personality newValue)
    {
        if (newValue == null) return;

        var oldName = Preferences.Get(SelectedPersonalityPrefKey, Personalities.First().Name);

        Preferences.Set(SelectedPersonalityPrefKey, newValue.Name);
        _personalityService.SetCurrent(newValue.Name);
        
        if (oldName != newValue.Name)
        {
            _log.LogInformation($"Personality switched from {oldName} to {newValue.Name}");    
        }
    }

    // ── Send ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SendAsync()
    {
        var text = PromptText;
        if (text.HasNoValue()) return;

        Messages.Add(new Message
                     {
                             Sender    = "user"
                           , Content   = text
                           , Timestamp = DateTime.Now
                     });

        PromptText = string.Empty;

        var modelToUse = "qwen2.5:14b";

        IsTyping = true;

        var assistantMsg = new Message
                           {
                                   Sender    = "assistant"
                                 , Content   = "thinking"
                                 , Timestamp = DateTime.Now
                           };
        Messages.Add(assistantMsg);

        _ = StartThinkingAsync(assistantMsg);

        // Tracks whether the API was actually reached so we only
        // refresh usage when a real Groq call may have been made.
        var reachedApi = false;

        try
        {
            var cp = _cpFactory.Create();

            await _apiHealthService.CheckApiAsync();

            if (_apiHealthService.IsApiAvailable.Not())
            {
                await _offlineQueueService.EnqueueAsync(ConversationId
                                                      , text
                                                      , modelToUse);
                await RefreshQueueStatusAsync();
                return;
            }

            reachedApi = true;

            if (UseStreaming.Not())
            {
                var response = await cp.ConverseAsync(text
                                                    , ConversationId
                                                    , modelToUse)
                                       .ConfigureAwait(false);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    assistantMsg.Content     = response.Message;
                    assistantMsg.WasFastPath = response.WasFastPath;
                });
            }
            else
            {
                var hasReceivedFirstChunk = false;

                await foreach (var chunk in cp.ConverseStreamAsync(text
                                                                  , ConversationId
                                                                  , modelToUse))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (hasReceivedFirstChunk.Not())
                        {
                            assistantMsg.Content  = string.Empty;
                            hasReceivedFirstChunk = true;
                        }

                        assistantMsg.Content += chunk;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // ConfigureAwait(false) means this catch runs on a thread-pool thread.
            // All UI-bound operations must be marshalled back to the main thread.
            var errorMessage = ex.Message;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                assistantMsg.Content = $"⚠ Error: {errorMessage}";
                Messages.Add(new Message
                             {
                                     Sender    = "system"
                                   , Content   = $"Error contacting CognitivePlatform:\n{errorMessage}"
                                   , Timestamp = DateTime.Now
                             });
            });
        }
        finally
        {
            StopThinking();

            // IsTyping is an [ObservableProperty] — setting it from a background thread
            // (after ConfigureAwait(false)) causes a WinUI cross-thread exception.
            MainThread.BeginInvokeOnMainThread(() => IsTyping = false);

            // Refresh usage after every turn that reached the API.
            // Non-fatal — runs fire-and-forget so it never delays the UI.
            if (reachedApi)
                _ = _appShellMasterViewModel.OnConversationTurnCompletedAsync();
        }
    }

    // ── Ancillary commands ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenKnowledge()
        => await Shell.Current.GoToAsync(nameof(KnowledgeInboxPage));

    [RelayCommand]
    public async Task NewChatAsync()
    {
        await _conversationMemory.ClearShortTermAsync();
        await _conversationMemory.ClearAsync();

        Messages.Clear();

        _log.LogInformation("Started new chat (STM cleared).");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task RefreshQueueStatusAsync()
        => PendingQueueCount = await _offlineQueueService.GetPendingCountAsync();

    private async Task RememberEntryAsync(Message message)
    {
        await _conversationMemory.AddAsync(message);
        await _memory.SaveEntryAsync(message.Sender
                                   , message.Content
                                   , message.Timestamp);
    }

    // ── Thinking animation ────────────────────────────────────────────────────
    
    private static string[] GenerateThinkingFrames(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            text = "...";

        var frames = new List<string>();

        // Forwards
        for (int i = 0; i < text.Length; i++)
        {
            var chars = text.ToCharArray();

            for (int j = 0; j < chars.Length; j++)
            {
                chars[j] = j == i
                                   ? char.ToUpper(chars[j])
                                   : char.ToLower(chars[j]);
            }

            frames.Add(new string(chars));
        }

        // Backwards
        for (int i = text.Length - 2; i > 0; i--)
        {
            var chars = text.ToCharArray();

            for (int j = 0; j < chars.Length; j++)
            {
                chars[j] = j == i
                                   ? char.ToUpper(chars[j])
                                   : char.ToLower(chars[j]);
            }

            frames.Add(new string(chars));
        }

        return frames.ToArray();
    }

    private static readonly string[] ThinkingWords =
    {
            // ── Tier 1: Highly Professional (always safe) ────────────────────────────
            "evaluating"
          , "analyzing"
          , "reasoning"
          , "synthesizing"
          , "structuring"
          , "deciphering"
          , "processing"
          , "calculating"
          , "deliberating"
          , "contemplating"
          , "pondering"
          , "reflecting"
          , "considering"

            // ── Tier 2: Professional but softer / abstract ───────────────────────────
          , "ruminating"
          , "cogitating"
          , "meditating"
          , "musing"
          , "speculating"
          , "surmising"
          , "conceiving"
          , "envisioning"
          , "envisaging"
          , "ideating"
          , "visualizing"
          , "brainstorming"
          , "mulling"
          , "cerebrating"
          , "deeming"
          , "opining"
          , "reckoning"

            // ── Tier 3: Neutral / approachable ───────────────────────────────────────
          , "crunching"
          , "decoding"
          , "unpacking"
          , "untangling"
          , "assembling-thoughts"
          , "lining-things-up"
          , "figuring-things-out"
          , "stitching-ideas"
          , "mapping-it-out"
          , "connecting-dots"
          , "spinning-gears"
          , "firing-neurons"
          , "working-on-it"
          , "hold-please"
          , "just-a-sec"
          , "trust-the-process"

            // ── Tier 4: Light personality / playful ──────────────────────────────────
          , "noodling"
          , "head-scratching"
          , "gear-turning"
          , "brain-juggling"
          , "mind-bubbling"
          , "idea-cooking"
          , "thought-wrangling"
          , "warming-up-circuits"
          , "charging-brain-cells"
          , "optimizing-thoughts"
          , "waking-the-neurons"
          , "aligning-weights"
          , "summoning-tokens"
          , "tickling-tensors"
          , "jiggling-bits"
          , "flipping-bits"
          , "simulating-intelligence"
          , "thinking-real-hard"
          , "doing-my-best"
          , "this-might-work"

            // ── Tier 5: Goofy / noticeable humor ─────────────────────────────────────
          , "thinkifying"
          , "brainstorminating"
          , "ponderfying"
          , "hamster-wheeling"
          , "brain-fizzling"
          , "think-o-matic-ing"
          , "vibing-with-data"
          , "magic-happening"
          , "calculating-ish"
          , "totally-thinking"
          , "probably-thinking"
          , "thinking...maybe"

            // ── Tier 6: Risky / ironic / least appropriate ───────────────────────────
          , "consulting-the-void"
          , "pretending-to-think"
          , "definitely-not-guessing"
          , "questionable-math"
          , "scheming"
          , "plotting"
          , "assuming"
          , "judging"
          , "thinking™"
    };



    private static readonly string[] ThinkingFrames =
    {
            "thinking"
          , "THinking"
          , "ThInking"
          , "ThiNking"
          , "ThinKing"
          , "ThinkIng"
          , "ThinkiNg"
          , "ThinkinG"
          , "ThinkiNg"
          , "ThinkIng"
          , "ThinKing"
          , "ThiNking"
          , "ThInking"
          , "THinking"
    };

    private CancellationTokenSource? _thinkingCts;

    private async Task StartThinkingAsync(Message assistantMsg, string[]? thinkingWords = null)
    {
        _thinkingCts = new CancellationTokenSource();
        var token    = _thinkingCts.Token;

        thinkingWords ??= ThinkingWords;

        try
        {
            var wordIndex  = 0;
            var frames     = GenerateThinkingFrames(thinkingWords[wordIndex]);
            var frameIndex = 0;

            var startedAt = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                var elapsed     = DateTime.UtcNow - startedAt;
                var elapsedText = FormatElapsedText(elapsed);
                var frame       = frames[frameIndex];

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!token.IsCancellationRequested)
                        assistantMsg.Content = $"{frame} ⏱ {elapsedText}";
                });

                frameIndex++;

                // ✅ Completed full animation cycle
                if (frameIndex >= frames.Length)
                {
                    frameIndex = 0;

                    // Move to next word
                    wordIndex = (wordIndex + 1) % thinkingWords.Length;

                    // Generate new frames for next word
                    frames = GenerateThinkingFrames(thinkingWords[wordIndex]);
                }

                await Task.Delay(120, token);
            }
        }
        catch (TaskCanceledException)
        {
            // Expected
        }
    }



    private static string FormatElapsedText(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 60)
        {
            return elapsed.TotalSeconds < 10
                       ? elapsed.TotalSeconds.ToString("0.0") + "s"
                       : elapsed.TotalSeconds.ToString("0")   + "s";
        }

        if (elapsed.TotalMinutes < 60)
            return $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";

        return $"{(int)elapsed.TotalHours:00}:{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
    }

    private void StopThinking()
    {
        _thinkingCts?.Cancel();
        _thinkingCts?.Dispose();
        _thinkingCts = null;
    }
}