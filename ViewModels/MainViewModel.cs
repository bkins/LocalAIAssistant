using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;

namespace LocalAIAssistant.ViewModels;

public partial class MainViewModel : ObservableObject
{

    private readonly ILlmService              _llmService;
    private readonly ILoggingService          _logger;
    private readonly IConversationMemory      _conversationMemory;
    private readonly IMemoryService           _memoryService;
    private readonly OllamaConfigService      _ollamaConfigService;

    public ApiEnvironmentDescriptor ApiEnvironmentDescriptor { get; }

    public ObservableCollection<string> Models { get; } = new(AvailableModels.Models);

    [ObservableProperty] private string selectedModel;
    [ObservableProperty] private string _promptText = string.Empty;

    public string LastResponse { get; set; } = string.Empty;

    public ObservableCollection<Message> Messages { get; } = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public MainViewModel (ILlmService              llmService
                        , ILoggingService          logger
                        , IConversationMemory      conversationMemory
                        , IMemoryService           memoryService
                        , OllamaConfigService      configService
                        , ApiEnvironmentDescriptor apiEnvironmentDescriptor)
    {
        _logger = logger;
        _logger.LogInformation("Initializing main view model");

        _llmService              = llmService;
        _conversationMemory      = conversationMemory;
        _memoryService           = memoryService;
        _ollamaConfigService     = configService;
        ApiEnvironmentDescriptor = apiEnvironmentDescriptor;
    }

    [RelayCommand]
    public async Task SendPromptAsync()
    {
        try
        {
            IsBusy = true;

            var prompt = PromptText?.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            // Create and store user message immediately
            var userPrompt = new Message
                             {
                                 Sender    = Senders.User
                               , Content   = prompt
                               , Timestamp = DateTime.UtcNow
                             };
            
            await RememberEntryAsync(userPrompt);

            Messages.Add(userPrompt);
            PromptText = string.Empty;

            // Create assistant placeholder for UI streaming
            var assistantMessage = new Message
                                   {
                                       Sender    = Senders.Ai
                                     , Content   = string.Empty
                                     , Timestamp = DateTime.UtcNow
                                   };
            
            Messages.Add(assistantMessage);

            // Run the streaming logic in background
            await Task.Run((Func<Task?>)(async () =>
            {
                var sb = new StringBuilder();

                await foreach (var chunk in _llmService.SendPromptStreamingAsync(prompt).ConfigureAwait(false))
                {
                    sb.Append(chunk);

                    // Update UI during streaming
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        assistantMessage.Content += chunk;
                    });
                }

                var finalResponse = sb.ToString();

                // Final UI update
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    assistantMessage.Content = finalResponse;
                });

                // Store *only* the polished AI response in memory
                var finalAssistantMessage = new Message
                                            {
                                                Sender    = Senders.Ai
                                              , Content   = finalResponse
                                              , Timestamp = DateTime.UtcNow
                                            };
                
                await RememberEntryAsync(finalAssistantMessage);
                
            }));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RememberEntryAsync(Message message)
    {
        await _conversationMemory.AddAsync(message);
        await _memoryService.SaveEntryAsync(message.Sender, message.Content, DateTime.UtcNow);
    }
    
    partial void OnSelectedModelChanged(string value)
    {
        if (value.HasValue())
        {
            _ollamaConfigService.SetModel(value);
        }
    }
}