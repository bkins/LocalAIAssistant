using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.Coco;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Core.Coco;
using LocalAIAssistant.Core.ConversationHistory;
using LocalAIAssistant.Core.Tts;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Core.BrainDump;
using LocalAIAssistant.Core.Media;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Services;
using LocalAIAssistant.Core.Conversation;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Google;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
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
    private readonly IConversationHistoryClient       _historyClient;
    private readonly ITtsService                      _ttsService;
    private readonly IGuidedBrainDumpFlow             _brainDumpFlow;
    private readonly IJournalApiClientFactory         _journalFactory;
    private readonly IMediaAttachmentApiClient        _mediaClient;
    private readonly ICocoApiClientFactory            _cocoFactory;
    private readonly IClipboardMonitorService         _clipboardMonitor;
    private readonly IGlobalHotkeyService             _hotkeyService;
    private readonly IGoogleCalendarService           _googleCalendar;

    // ── Connectivity ──────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isOffline;

    // ── UI state ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool        _useStreaming        = true;
    [ObservableProperty] private string      _promptText          = string.Empty;
    [ObservableProperty] private bool        _isBusy;
    [ObservableProperty] private bool        _isTyping;
    [ObservableProperty] private Personality _selectedPersonality;
    [ObservableProperty] private int         _pendingQueueCount;
    [ObservableProperty] private bool        _showStreamingOption = false;
    [ObservableProperty] private bool        _isCocoMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasClipboardNotice))]
    private string _clipboardNoticeText = string.Empty;

    public bool HasClipboardNotice => !string.IsNullOrEmpty(_clipboardNoticeText);

    public bool IsCocoToggleVisible =>
        DeviceInfo.Current.Platform == DevicePlatform.WinUI
     && Preferences.Default.Get(StringConsts.CocoEnabledPrefKey, false);

    // ── TTS state ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _ttsIsAvailable;
    [ObservableProperty] private bool   _ttsIsEnabled;
    [ObservableProperty] private string _ttsSelectedVoice = string.Empty;

    public ObservableCollection<string> TtsVoices { get; } = new();

    public string SuggestedModelToUse { get; set; } = "phi-3:mini";

    public ObservableCollection<Message>     Messages      { get; } = new();
    public ObservableCollection<Personality> Personalities { get; } = new();

    public string ConversationId    { get; private set; }
    public bool   HasBeenInitialized { get; private set; }

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
                        , AppShellMasterViewModel          appShellMasterViewModel
                        , IConversationHistoryClient       historyClient
                        , ITtsService                      ttsService
                        , IGuidedBrainDumpFlow             brainDumpFlow
                        , IJournalApiClientFactory         journalFactory
                        , IMediaAttachmentApiClient        mediaClient
                        , ICocoApiClientFactory            cocoFactory
                        , IClipboardMonitorService         clipboardMonitor
                        , IGlobalHotkeyService             hotkeyService
                        , IGoogleCalendarService           googleCalendar )
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
        _historyClient           = historyClient;
        _ttsService              = ttsService;
        _brainDumpFlow           = brainDumpFlow;
        _journalFactory          = journalFactory;
        _mediaClient             = mediaClient;
        _cocoFactory             = cocoFactory;
        _clipboardMonitor        = clipboardMonitor;
        _hotkeyService           = hotkeyService;
        _googleCalendar          = googleCalendar;

        _clipboardMonitor.CodeDetected += OnCodeDetectedInClipboard;
        _hotkeyService.HotkeyPressed   += OnCocoHotkeyPressed;

        // ENH-20: persist ConversationId across app restarts so server history can be retrieved.
        var savedConversationId = Preferences.Get(StringConsts.ActiveConversationIdKey, string.Empty);
        if (string.IsNullOrEmpty(savedConversationId))
        {
            savedConversationId = Guid.NewGuid().ToString();
            Preferences.Set(StringConsts.ActiveConversationIdKey, savedConversationId);
        }
        ConversationId = savedConversationId;

        _apiState.ConnectivityChanged += (_, _) => IsOffline = _apiState.IsOffline;
    }

    public async Task InitializeAsync()
    {
        HasBeenInitialized = false;
        Messages.Clear();

        // ENH-20: server-first rehydration. On any exception fall back to local STM silently.
        var serverLoaded = false;

        try
        {
            var turns = await _historyClient.GetHistoryAsync(ConversationId);
            if (turns.Count > 0)
            {
                foreach (var turn in turns)
                {
                    Messages.Add(new Message
                                 {
                                         Sender         = turn.Role
                                       , Content        = turn.Content
                                       , Timestamp      = turn.Timestamp.LocalDateTime
                                       , ConversationId = ConversationId
                                 });
                }
                serverLoaded = true;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Server history unavailable — fall through to local STM.
            _log.LogError(ex, "Failed to load conversation history from server; falling back to local STM");
        }

        if (!serverLoaded)
        {
            var stm = await _conversationMemory.LoadShortTermAsync();
            foreach (var message in stm.OrderBy(message => message.Timestamp))
                Messages.Add(message);
        }

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

        await InitializeTtsAsync();
        InitializeCocoSidecar();

        HasBeenInitialized = true;
    }

    private void InitializeCocoSidecar()
    {
        var cocoEnabled             = Preferences.Default.Get(StringConsts.CocoEnabledPrefKey, false);
        var clipboardMonitorEnabled = Preferences.Default.Get(StringConsts.CocoClipboardMonitorEnabledPrefKey, true);

        _clipboardMonitor.IsEnabled = cocoEnabled && clipboardMonitorEnabled;
        if (cocoEnabled && clipboardMonitorEnabled)
            _clipboardMonitor.Start();

        if (cocoEnabled)
        {
            var hotkey = Preferences.Default.Get(StringConsts.CocoHotkeyPrefKey, StringConsts.CocoDefaultHotkey);
            _hotkeyService.Register(hotkey);
        }
    }

    private async Task InitializeTtsAsync()
    {
        TtsIsAvailable = _ttsService.IsTtsAvailable;
        if (!TtsIsAvailable) return;

        TtsIsEnabled = _ttsService.IsEnabled;

        var voices = await _ttsService.GetVoicesAsync();
        TtsVoices.Clear();
        foreach (var voice in voices)
            
            TtsVoices.Add(voice.Name);

        var preferred = _ttsService.PreferredVoiceName;
        if (preferred is not null && TtsVoices.Contains(preferred))
        {
            TtsSelectedVoice = preferred;
        }
        else
        {
            var currentLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var bestVoice       = VoiceSelector.SelectVoice(voices, null, currentLanguage);

            TtsSelectedVoice                = bestVoice?.Name ?? TtsVoices.FirstOrDefault() ?? string.Empty;
            _ttsService.PreferredVoiceName  = TtsSelectedVoice;
        }
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

    partial void OnTtsIsEnabledChanged(bool value)
        => _ttsService.IsEnabled = value;

    partial void OnTtsSelectedVoiceChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
            _ttsService.PreferredVoiceName = value;
    }

    public Task StopSpeakingAsync() => _ttsService.StopAsync();

    // ── Send ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SendAsync()
    {
        var text = PromptText?.Trim();
        if (text.HasNoValue()) return;

        var userMessage = new Message
                          {
                                  Sender         = "user"
                                , Content        = text
                                , Timestamp      = DateTime.Now
                                , ConversationId = ConversationId
                          };
        Messages.Add(userMessage);

        PromptText = string.Empty;

        // BUG-32: persist user message to client-side STM so the conversation survives app restart.
        // Done before the API call so a network failure doesn't lose the user's typed input.
        await PersistToShortTermAsync(userMessage);

        var modelToUse = "qwen2.5:14b";

        IsTyping = true;

        var assistantMsg = new Message
                           {
                                   Sender         = "assistant"
                                 , Content        = "thinking"
                                 , Timestamp      = DateTime.Now
                                 , ConversationId = ConversationId
                           };
        Messages.Add(assistantMsg);

        _ = StartThinkingAsync(assistantMsg);

        // Tracks whether the API was actually reached so we only
        // refresh usage when a real Groq call may have been made.
        var reachedApi = false;

        // BUG-32: only persist the assistant message on real success branches.
        // 429, exception, and offline-enqueue paths leave assistantMsg as transient UI ephemera.
        var assistantSucceeded = false;

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

            // ENH-32: intercept brain dump flow turns before normal converse routing.
            if (_brainDumpFlow.IsActive || _brainDumpFlow.IsTrigger(text!))
            {
                await HandleBrainDumpTurnAsync(text!, assistantMsg, cp, modelToUse);
                assistantSucceeded = true;
                return;
            }

            // ENH-33: intercept media attachment intent before sending to LLM.
            if (IsMediaAttachTrigger(text!))
            {
                await HandleMediaAttachAsync(assistantMsg);
                assistantSucceeded = true;
                return;
            }

            // Intercept calendar actions when no Google Calendar token exists.
            if (IsCalendarActionTrigger(text!) && !_googleCalendar.HasToken)
            {
                await HandleCalendarConnectPromptAsync(assistantMsg);
                assistantSucceeded = true;
                return;
            }

            // Coco routing: explicit mode toggle OR auto-detected code query (Phase 2).
            var cocoEnabled    = Preferences.Default.Get(StringConsts.CocoEnabledPrefKey, false);
            var isAutoCodeQuery = cocoEnabled
                               && !CodeIntentAnalyzer.IsExplicitCpRequest(text!)
                               && CodeIntentAnalyzer.IsCodeQuery(text!);

            if (IsCocoMode || isAutoCodeQuery)
            {
                await HandleCocoTurnAsync(text!, assistantMsg, wasAutoRouted: isAutoCodeQuery && !IsCocoMode);
                assistantSucceeded = true;
                return;
            }

            if (UseStreaming.Not())
            {
                var response = await cp.ConverseAsync(text
                                                    , ConversationId
                                                    , modelToUse)
                                       .ConfigureAwait(false);

                if (response.Message.Contains("Groq API returned 429", StringComparison.CurrentCultureIgnoreCase))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        //assistantMsg.Content = $"⚠ {response.Message}";
                        Messages.Add(new Message
                                     {
                                             Sender    = "system"
                                           , Content   = $"⚠ API rate limit reached:\n{response.Message}"
                                           , Timestamp = DateTime.Now
                                     });

                    });
                }
                else
                {
                    string  cleanMessage;
                    string? tierNotice;

                    if (response.ModelNotice is not null)
                    {
                        cleanMessage = response.Message;
                        tierNotice   = response.ModelNotice;
                    }
                    else
                    {
                        (cleanMessage, tierNotice) = TierNoticeExtractor.Extract(response.Message);
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        assistantMsg.Content     = cleanMessage;
                        assistantMsg.WasFastPath = response.WasFastPath;
                        if (tierNotice is not null)
                            assistantMsg.TierNotice = tierNotice;

                        foreach (var insight in response.Insights)
                        {
                            Messages.Add(new Message
                                         {
                                                 Sender         = "assistant"
                                               , Content        = insight.Message
                                               , IsInsight      = true
                                               , Timestamp      = DateTime.Now
                                               , ConversationId = ConversationId
                                         });
                        }
                    });

                    if (tierNotice is not null)
                        ScheduleTierNoticeDismiss(assistantMsg);

                    assistantSucceeded = true;
                }
            }
            else
            {
                var hasReceivedFirstChunk = false;

                await foreach (var chunk in cp.ConverseStreamAsync(text
                                                                  , ConversationId
                                                                  , modelToUse))
                {
                    // Stop the thinking animation as soon as the first real chunk arrives so it
                    // cannot overwrite streamed content. Cancel() is thread-safe per CTS docs.
                    if (hasReceivedFirstChunk.Not())
                        _thinkingCts?.Cancel();

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

                // Flush queued chunk callbacks so assistantMsg.Content reflects the final stream state
                // before persisting. BeginInvokeOnMainThread runs FIFO on the dispatcher — awaiting an
                // empty InvokeOnMainThreadAsync ensures all prior chunks have been applied.
                await MainThread.InvokeOnMainThreadAsync(() => { });
                assistantSucceeded = true;
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

        if (assistantSucceeded)
        {
            await PersistToShortTermAsync(assistantMsg);
            _ = _ttsService.SpeakAsync(assistantMsg.Content);
        }
    }

    // ── Ancillary commands ────────────────────────────────────────────────────

    [RelayCommand]
    public async Task ClearMessagesAsync()
    {
        await _ttsService.StopAsync();
        Messages.Clear();
    }

    [RelayCommand]
    private async Task OpenKnowledge()
        => await Shell.Current.GoToAsync(nameof(KnowledgeInboxPage));

    [RelayCommand]
    public async Task NewChatAsync()
    {
        await _conversationMemory.ClearShortTermAsync();
        await _conversationMemory.ClearAsync();

        Messages.Clear();

        // ENH-20 fix: rotate the ConversationId so subsequent sends and the next
        // InitializeAsync load start a fresh server-side thread, not the old one.
        var newConversationId = Guid.NewGuid().ToString();
        Preferences.Set(StringConsts.ActiveConversationIdKey, newConversationId);
        ConversationId = newConversationId;

        _log.LogInformation("Started new chat (STM cleared, ConversationId rotated).");
    }

    public async Task SwitchConversationAsync(string conversationId)
    {
        Messages.Clear();

        Preferences.Set(StringConsts.ActiveConversationIdKey, conversationId);
        ConversationId = conversationId;

        // InitializeAsync falls back to STM when the server is unreachable. Clear STM
        // only after the load so offline users see their locally-persisted history
        // rather than an empty chat.
        await InitializeAsync();

        await _conversationMemory.ClearShortTermAsync();
        await _conversationMemory.ClearAsync();
    }

    // ── Google Calendar connect flow ──────────────────────────────────────────

    private static bool IsCalendarActionTrigger(string input)
    {
        var lower = input.ToLowerInvariant();
        return lower.Contains("add to calendar")
            || lower.Contains("schedule event")
            || lower.Contains("add event")
            || lower.Contains("calendar event")
            || lower.Contains("put on calendar")
            || lower.Contains("add appointment")
            || lower.Contains("schedule meeting")
            || lower.Contains("create event");
    }

    private async Task HandleCalendarConnectPromptAsync(Message assistantMsg)
    {
        var hasClientId = !string.IsNullOrWhiteSpace(_googleCalendar.ClientId);

        var content = hasClientId
            ? "To use calendar features, connect your Google Calendar. Tap the button below to connect."
            : "To use calendar features, add your Google Calendar Client ID in Settings, then tap connect.";

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            assistantMsg.Content                 = content;
            assistantMsg.IsCalendarConnectPrompt = hasClientId;
        });
    }

    [RelayCommand]
    public async Task ConnectGoogleCalendarAsync()
    {
        var success = await _googleCalendar.ConnectAsync();

        var content = success
            ? "Google Calendar connected! Your calendar actions will now work."
            : "Calendar connection failed. Make sure your Client ID is set in Settings and try again.";

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new Message
                         {
                                 Sender    = "assistant"
                               , Content   = content
                               , Timestamp = DateTime.Now
                         });
        });
    }

    // ── Media attachment flow ─────────────────────────────────────────────────

    private static bool IsMediaAttachTrigger(string input)
    {
        var lower = input.ToLowerInvariant();
        return lower.Contains("attach photo")
            || lower.Contains("attach image")
            || lower.Contains("attach picture")
            || lower.Contains("add photo to journal")
            || lower.Contains("add image to journal")
            || lower.Contains("add picture to journal")
            || lower.Contains("add photo to last entry")
            || lower.Contains("add image to last entry");
    }

    private async Task HandleMediaAttachAsync(Message assistantMsg)
    {
        FileResult? photo = null;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                photo = await MediaPicker.Default.PickPhotoAsync();
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                assistantMsg.Content = $"Could not open the photo picker: {ex.Message}. Open a journal entry and use the Attach button instead.";
            });
            return;
        }

        if (photo is null)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                assistantMsg.Content = "No photo selected.";
            });
            return;
        }

        var journalClient = _journalFactory.Create();
        var recentEntry   = await journalClient.GetMostRecentAsync();

        if (recentEntry is null)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                assistantMsg.Content = "Could not find a journal entry to attach to. Open a journal entry and use the Attach button.";
            });
            return;
        }

        await using var stream      = await photo.OpenReadAsync();
        var             contentType = photo.ContentType ?? "image/jpeg";
        // BUG-17 audit: recentEntry.Id (Guid) is passed to the API but never surfaced in the
        // confirmation message — the friendly date format is used instead. No UUID leakage.
        var uploaded = await _mediaClient.UploadAsync(recentEntry.Id
                                                    , photo.FileName
                                                    , contentType
                                                    , stream);

        var confirmation = uploaded is not null
            ? $"Photo attached to your journal entry from {recentEntry.CreatedAt:MMMM d}."
            : "Upload failed. Please try again or use the Attach button on the journal entry.";

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            assistantMsg.Content = confirmation;
        });
    }

    // ── Brain dump flow ───────────────────────────────────────────────────────

    private async Task HandleBrainDumpTurnAsync( string                    input
                                               , Message                   assistantMsg
                                               , CognitivePlatformClientBase cp
                                               , string                    model )
    {
        FlowTurn turn;

        if (_brainDumpFlow.IsActive)
        {
            turn = await _brainDumpFlow.HandleInputAsync(input);
        }
        else
        {
            // Starting a new session — wire the converse delegate for LLM extraction
            Func<string, CancellationToken, Task<string>> converseFn =
                async (prompt, ct) =>
                {
                    var result = await cp.ConverseAsync(prompt, ConversationId, model)
                                         .ConfigureAwait(false);
                    return result.Message;
                };

            turn = await _brainDumpFlow.StartAsync(converseFn);
        }

        await MainThread.InvokeOnMainThreadAsync(() => assistantMsg.Content = turn.Message);

        // When an item was confirmed as a task, create it via the NL action silently.
        // Fire-and-forget: the chat message already tells the user it was queued.
        if (turn.Action == FlowAction.CreateTask && turn.TaskTitle is not null)
        {
            _ = cp.ConverseAsync($"add task: {turn.TaskTitle}", ConversationId, model);
        }
    }

    // ── Coco code-intelligence flow ───────────────────────────────────────────

    private async Task HandleCocoTurnAsync( string  question
                                          , Message assistantMsg
                                          , bool    wasAutoRouted = false )
    {
        var coco                   = _cocoFactory.Create();
        var receivedAnswer         = false;
        var stoppedThinkingForCoco = false;

        try
        {
            await foreach (var ev in coco.AskStreamAsync(question))
            {
                if (ev.IsHeartbeat) continue;

                if (ev.IsComplete)
                {
                    var answer  = ev.Response ?? string.Empty;
                    var sources = ev.Sources;

                    if (sources?.Count > 0)
                    {
                        var sourceLines = string.Join("\n", sources.Select(src => $"— `{src}`"));
                        answer = $"{answer}\n\n**Sources:**\n{sourceLines}";
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        assistantMsg.Content   = answer;
                        assistantMsg.IsViaCoco = true;
                        if (sources?.Count > 0)
                            assistantMsg.CocoSources = sources;
                    });

                    receivedAnswer = true;
                    break;
                }

                // Stage progress event — stop the generic thinking animation and show the
                // Coco pipeline stage so the user sees what's happening (searching, analyzing…).
                if (!stoppedThinkingForCoco)
                {
                    _thinkingCts?.Cancel();
                    stoppedThinkingForCoco = true;
                }

                var stageLabel = ev.Detail ?? ev.Stage;
                if (stageLabel is not null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        assistantMsg.Content = $"🔵 {stageLabel}…";
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                assistantMsg.Content = $"⚠ Coco error: {ex.Message}";
            });
            return;
        }

        if (!receivedAnswer)
        {
            var baseUrl = Preferences.Default.Get(StringConsts.CocoBaseUrlPrefKey, StringConsts.CocoDefaultBaseUrl);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                assistantMsg.Content = $"⚠ Coco is unavailable. Check that the Coco API is running at {baseUrl}.";
            });
        }
    }

    // ── Clipboard & hotkey sidecar handlers ──────────────────────────────────

    private async void OnCodeDetectedInClipboard(object? sender, string code)
    {
        var cocoEnabled             = Preferences.Default.Get(StringConsts.CocoEnabledPrefKey, false);
        var clipboardMonitorEnabled = Preferences.Default.Get(StringConsts.CocoClipboardMonitorEnabledPrefKey, true);

        if (!cocoEnabled || !clipboardMonitorEnabled) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            PromptText          = $"Explain this code:\n\n{code}";
            IsCocoMode          = true;
            ClipboardNoticeText = "Code detected in clipboard — ready to ask Coco";

            await Task.Delay(3000);
            ClipboardNoticeText = string.Empty;
        });
    }

    private void OnCocoHotkeyPressed(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsCocoMode = true;
        });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task RefreshQueueStatusAsync()
        => PendingQueueCount = await _offlineQueueService.GetPendingCountAsync();

    private async Task PersistToShortTermAsync(Message message)
    {
        try
        {
            await _conversationMemory.AddAsync(message);
        }
        catch (Exception ex)
        {
            // STM persistence is non-fatal — the message is still in the in-memory
            // Messages collection for the current session. Log and continue so a
            // SQLite hiccup doesn't break the chat UX.
            _log.LogError(ex, "Failed to persist message to short-term memory");
        }
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
          , "pontificating"

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
          , "mustering"
          , "sussing"

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

    // ── Tier notice auto-dismiss ──────────────────────────────────────────────

    private static void ScheduleTierNoticeDismiss(Message msg, int delayMs = 5000)
    {
        _ = Task.Delay(delayMs).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() => msg.TierNotice = null));
    }
}