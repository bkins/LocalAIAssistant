using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CP.Client.Core.Common.ConectivityToApi;
using LocalAIAssistant.Avails.ThinkingAnimation;
using LocalAIAssistant.CognitivePlatform;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Knowledge.Inbox;
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

    public  bool    IsOffline => _apiState.IsOffline;
    
    private Thinker thinking;
    public  string  SuggestedModelToUse { get; set; } = "phi-3:mini";

    [ObservableProperty] private bool   _useStreaming = false;
    [ObservableProperty] private string _promptText   = string.Empty;
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private bool   _isTyping;


    public ObservableCollection<Message>     Messages       { get; } = new();
    public ObservableCollection<Personality> Personalities  { get; } = new();

    [ObservableProperty] private Personality _selectedPersonality;
    
    public string ConversationId { get; } = Guid.NewGuid().ToString();

    public Action ScrollToBottomRequested { get; set; }

    public ICommand SendCommand             { get; }
    public ICommand SendForStreamingCommand { get; }
    // public ICommand OpenKnowledgeCommand    { get; }

    public ChatViewModel (ILlmService                      llmService
                        , IConversationMemory              conversationMemory
                        , IMemoryService                   memory
                        , IPersonalityService              personalityService
                        , ILoggingService                  log
                        , IOptions<MemoryRetrievalOptions> memoryOptions
                        , IOrchestratorService             orchestrator
                        , ICognitivePlatformClientFactory  cpFactory
                        , IConnectivityState               apiState)
    {
        _llmService             = llmService;
        _conversationMemory     = conversationMemory;
        _memory                 = memory;
        _personalityService     = personalityService;
        _log                    = log;
        _memoryRetrievalOptions = memoryOptions;
        _orchestrator           = orchestrator;
        _cpFactory              = cpFactory;
        _apiState               = apiState;

        _apiState.ConnectivityChanged += (_
                                        , _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(IsOffline));
            });
        };

        SendCommand             = new AsyncRelayCommand(SendAsync);
        SendForStreamingCommand = new AsyncRelayCommand(SendPromptForStreamingAsync);

    }

    private async Task SendAsync()
    {
        _log.LogInformation($"UseStreaming = {UseStreaming}");

        await SendPromptAsync();
    }

    private async Task SentForStreamingAsync()
    {
        await SendPromptForStreamingAsync();
    }
    
    public async Task InitializeAsync()
    {
        // 1) Load STM messages into the UI
        var stm = await _conversationMemory.LoadShortTermAsync();
        
        Messages.Clear();
        
        foreach (var m in stm.OrderBy(m => m.Timestamp))
        {
            Messages.Add(m);
        }

        // 2) Load personalities from your service (fallback if empty)
        Personalities.Clear();
        
        var allPersonalities = _personalityService.GetAll();
        if (allPersonalities is { Count: > 0 })
        {
            foreach (var personality in allPersonalities) Personalities.Add(personality);
        }
        else
        {
            // Safety fallback so the UI works
            Personalities.Add(new Personality
                              {
                                  Name         = "The Barista"
                                , SystemPrompt = "You are Jenny, a friendly coffee barista who remembers regulars."
                                , Description  = "Warm, upbeat coffee expert."
                                , IsDefault    = true
                              });
        }

        // 3) Restore previously selected personality
        var last = Preferences.Get(SelectedPersonalityPrefKey, Personalities.First().Name);
        SelectedPersonality = Personalities.FirstOrDefault(personality => personality.Name == last) 
                           ?? Personalities.First();
        _personalityService.SetCurrent(SelectedPersonality.Name);
        
    }
    
    partial void OnSelectedPersonalityChanged(Personality newValue)
    {
        if (newValue == null) return;
        
        Preferences.Set(SelectedPersonalityPrefKey, newValue.Name);
        
        var oldValue = Preferences.Get(SelectedPersonalityPrefKey,  Personalities.First().Name);
        
        _personalityService.SetCurrent(newValue.Name);
        _log.LogInformation($"Personality switched from {oldValue} to {newValue.Name}");
    }

    [RelayCommand]
    public async Task SendPromptAsync()
    {
        var hasReceivedFirstOutput = false;

        var text = PromptText;
        if (text.HasNoValue()) return;

        Messages.Add(new Message
                     {
                             Sender    = "user"
                           , Content   = text
                           , Timestamp = DateTime.Now
                     });
        
        PromptText = "";
        
        ScrollToBottomRequested?.Invoke();

        var modelToUse = "llama3"; //"mistral:latest"; //

        IsTyping = true;

        try
        {
            var assistantMsg = new Message
                               {
                                       Sender    = "assistant"
                                     , Content   = "thinking"
                                     , Timestamp = DateTime.Now
                               };
            Messages.Add(assistantMsg);

            _ = StartThinkingAsync(assistantMsg);

            if (!UseStreaming)
            {
                var cp = _cpFactory.Create();
                var response = await cp.ConverseAsync(text
                                                    , ConversationId
                                                    , modelToUse)
                                       .ConfigureAwait(false);
                
                assistantMsg.Content = response.Message;
            }
            else
            {
                var cp = _cpFactory.Create();
                await foreach (var chunk in cp.ConverseStreamAsync(text
                                                                    , ConversationId
                                                                  , modelToUse))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (hasReceivedFirstOutput.Not())
                        {
                            assistantMsg.Content   = string.Empty;
                            hasReceivedFirstOutput = true;
                        }

                        assistantMsg.Content += chunk;
                        ScrollToBottomRequested?.Invoke();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new Message
                         {
                                 Sender = "system"
                               , Content = $"Error contacting CognitivePlatform:\n{ex.Message}"
                               , Timestamp = DateTime.Now
                         });
        }
        finally
        {
            StopThinking();   // 🔒 ALWAYS stop
            IsTyping = false; // 🔒 ALWAYS clear busy state
        }
        
        ScrollToBottomRequested?.Invoke();
    }

    //
    // [RelayCommand]
    // public async Task SendPromptAsync()
    // {
    //     var text = PromptText;
    //     if (string.IsNullOrWhiteSpace(text)) return;
    //
    //     // Add user message to UI
    //     Messages.Add(new Message
    //                  {
    //                          Sender = "user"
    //                        , Content = text
    //                        , Timestamp = DateTime.Now
    //                  });
    //
    //     PromptText = "";
    //     ScrollToBottomRequested?.Invoke();
    //
    //     IsTyping = true;
    //     
    //     try
    //     {
    //         var response = await _cp.ConverseAsync(text
    //                                              , "default"
    //                                              , "llama3"); //"phi-3:mini"); //TODO: make `model` param defined by user
    //
    //         Messages.Add(new Message
    //                      {
    //                              Sender    = "assistant",
    //                              Content   = response.Message,
    //                              Timestamp = DateTime.Now
    //                      });
    //     }
    //     catch (Exception ex)
    //     {
    //         Messages.Add(new Message
    //                      {
    //                              Sender    = "system",
    //                              Content   = $"Error contacting CognitivePlatform:\n{ex.Message}",
    //                              Timestamp = DateTime.Now
    //                      });
    //     }
    //     finally
    //     {
    //         IsTyping = false;
    //     }
    //     ScrollToBottomRequested?.Invoke();
    // }

    
    [RelayCommand]
    public async Task SendPromptForStreamingAsync()
    {
        var text = PromptText;
        if (string.IsNullOrWhiteSpace(text)) return;

        // Add user message to UI
        Messages.Add(new Message
                     {
                             Sender = "user"
                           , Content = text
                           , Timestamp = DateTime.Now
                     });

        PromptText = "";
        ScrollToBottomRequested?.Invoke();

        IsTyping = true;
        
        try
        {
            var assistantMsg = new Message
                               {
                                       Sender = "assistant"
                                     , Content = ""
                               };

            Messages.Add(assistantMsg);

            var cp = _cpFactory.Create();
            await foreach (var chunk in cp.ConverseStreamAsync(text
                                                             , ConversationId
                                                             , "llama3")) //"phi-3:mini"); //TODO: make `model` param defined by user)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    assistantMsg.Content += chunk;
                    ScrollToBottomRequested?.Invoke();
                });
            }

        }
        catch (Exception ex)
        {
            Messages.Add(new Message
                         {
                                 Sender = "system"
                               , Content = $"Error contacting CognitivePlatform:\n{ex.Message}"
                               , Timestamp = DateTime.Now
                         });
        }
        finally
        {
            IsTyping = false;
        }
        
        ScrollToBottomRequested?.Invoke();
    }

    [RelayCommand]
    private async Task OpenKnowledge()
    {
        await Shell.Current.GoToAsync(nameof(KnowledgeInboxPage));

    }
    [RelayCommand]
    private async Task SendPromptAsync_old()
    {
        var prompt = PromptText?.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
            return;

        try
        {
            IsBusy = true;

            // Ensure the currently selected personality is active
            if (SelectedPersonality != null)
                _personalityService.SetCurrent(SelectedPersonality.Name);

            // --- Create user message ---
            var userMsg = new Message
                          {
                              Sender         = Senders.User
                            , Content        = prompt
                            , Timestamp      = DateTime.UtcNow
                            , ConversationId = ConversationId
                          };

            Messages.Add(userMsg);
            await RememberEntryAsync(userMsg);
           
            PromptText = string.Empty;

            // --- Prepare assistant placeholder message ---
            var assistantMsg = new Message
                               {
                                       Sender         = Senders.Assistant
                                     , Content        = string.Empty
                                     , Timestamp      = DateTime.UtcNow
                                     , ConversationId = ConversationId
                               };
            Messages.Add(assistantMsg);

            var sb = new StringBuilder();

            // --- Stream AI response safely ---
            // The await foreach loop handles the background task implicitly.
            // No Task.Run() is needed here.
            await foreach (var chunk in _orchestrator.ProcessAsync(userMsg.Content, SelectedPersonality)
                                                     .ConfigureAwait(false))
            {
                sb.Append(chunk);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    assistantMsg.Content += chunk;
                    ScrollToBottomRequested?.Invoke();
                });
            }

            // --- Save final AI response to memory ---
            await RememberEntryAsync(assistantMsg);
        }
        catch (Exception ex)
        {
            _log.LogError(ex
                        , "Error in SendPromptAsync");
            // It's good practice to show the user the error
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Messages.Add(new Message
                             {
                                 Sender         = Senders.Assistant
                               , Content        = $"[Error] An unexpected error occurred: {ex.Message}"
                               , Timestamp      = DateTime.UtcNow
                               , ConversationId = ConversationId
                             });
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    public async Task NewChatAsync()
    {
        // Clear current session (STM + UI)
        await _conversationMemory.ClearShortTermAsync();
        await _conversationMemory.ClearAsync();
        
        Messages.Clear();
        
        _log.LogInformation("Started new chat (STM cleared).");
    }

    private async Task RememberEntryAsync(Message message)
    {
        await _conversationMemory.AddAsync(message);
        await _memory.SaveEntryAsync(message.Sender
                                   , message.Content
                                   , message.Timestamp);
    }

    private static readonly string[] ThinkingFrames =
    {
            "thinking",
            "Thinking",
            "tHinking",
            "thInking",
            "thiNking",
            "thinKing",
            "thinkIng",
            "thinkiNg",
            "thinkinG",
    };

    private CancellationTokenSource? _thinkingCts;

    private async Task StartThinkingAsync(Message assistantMsg)
    {
        _thinkingCts = new CancellationTokenSource();
        var token = _thinkingCts.Token;

        try
        {
            var index      = 0;
            var _startedAt = DateTime.UtcNow;
            
            while (!token.IsCancellationRequested)
            {
                var    elapsed = DateTime.UtcNow - _startedAt;
                string elapsedText;

                elapsedText = FormatElapsedText(elapsed);
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    assistantMsg.Content = $"{ThinkingFrames[index]} ⏱ {elapsedText}";
                });

                index = (index + 1) % ThinkingFrames.Length;

                await Task.Delay(120, token);
            }
        }
        catch (TaskCanceledException)
        {
            // expected
        }
    }

    private static string FormatElapsedText (TimeSpan elapsed)
    {

        string elapsedText;
        if (elapsed.TotalSeconds < 60)
        {
            // s.0 -> ss
            elapsedText = elapsed.TotalSeconds < 10
                                  ? elapsed.TotalSeconds.ToString("0.0") + "s"
                                  : elapsed.TotalSeconds.ToString("0")   + "s";
        }
        else if (elapsed.TotalMinutes < 60)
        {
            // mm:ss
            elapsedText = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        }
        else
        {
            // hh:mm:ss
            elapsedText = $"{(int)elapsed.TotalHours:00}:{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        }

        return elapsedText;
    }

    private void StopThinking()
    {
        _thinkingCts?.Cancel();
        _thinkingCts?.Dispose();
        _thinkingCts = null;
    }
}
