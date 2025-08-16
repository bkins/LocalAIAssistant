using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;

namespace LocalAIAssistant.ViewModels;

public partial class MainViewModel : ObservableObject
{

    private readonly ILlmService         _llmService;
    private readonly ILoggingService     _logger;
    private readonly IConversationMemory _conversationMemory;
    private readonly IMemoryService      _memoryService;

    [ObservableProperty] private string _promptText = string.Empty;

    public string LastResponse { get; set; } = string.Empty;

    public ObservableCollection<Message> Messages { get; } = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public MainViewModel(ILlmService         llmService
                       , ILoggingService     logger
                       , IConversationMemory conversationMemory
                       , IMemoryService      memoryService)
    {
        _logger = logger;
        _logger.LogInformation("Initializing main view model");

        _llmService         = llmService;
        _conversationMemory = conversationMemory;
        _memoryService      = memoryService;
    }


    // [RelayCommand]
    // public async Task SendPromptAsync()
    // {
    //     try
    //     {
    //         IsBusy = true;
    //     
    //         var prompt = PromptText?.Trim();
    //         if (string.IsNullOrEmpty(prompt)) return;
    //         var userPrompt = new Message { Sender = "User", Content = prompt };
    //     
    //         await _conversationMemory.AddAsync(userPrompt);
    //     
    //         Messages.Add(userPrompt);
    //         PromptText = string.Empty;
    //
    //         var assistantMessage = new Message { Sender = "AI", Content = string.Empty };
    //         Messages.Add(assistantMessage);
    //
    //         // Now stream and update UI in real-time
    //         _ = Task.Run(async () =>
    //         {
    //             var sb = new StringBuilder();
    //
    //             await foreach (var chunk in _llmService.SendPromptStreamingAsync(prompt))
    //             {
    //                 sb.Append(chunk);
    //
    //                 // Update UI
    //                 MainThread.BeginInvokeOnMainThread(() =>
    //                 {
    //                     assistantMessage.Content += chunk;
    //                 });
    //             }
    //
    //             // Ensure logging happens with the complete response
    //             var finalResponse = sb.ToString();
    //             assistantMessage.Content = finalResponse;
    //         
    //         
    //             await _conversationMemory.AddAsync(assistantMessage);
    //         });
    //     }
    //     finally
    //     {
    //         _isBusy = false;    
    //     }
    // }
    // [RelayCommand]
    // public async Task SendPromptAsync()
    // {
    //     try
    //     {
    //         IsBusy = true;
    //
    //         var prompt = PromptText?.Trim();
    //         if (string.IsNullOrEmpty(prompt)) return;
    //         var userPrompt = new Message { Sender = "User", Content = prompt };
    //
    //         await _conversationMemory.AddAsync(userPrompt);
    //
    //         Messages.Add(userPrompt);
    //         PromptText = string.Empty;
    //
    //         var assistantMessage = new Message { Sender = "AI", Content = string.Empty };
    //         Messages.Add(assistantMessage);
    //
    //         // Run the streaming logic on a background thread
    //         await Task.Run(async () =>
    //         {
    //             var sb = new StringBuilder();
    //
    //             // Use ConfigureAwait(false) to prevent the await foreach from
    //             // trying to return to the UI thread, allowing the background
    //             // operation to continue uninterrupted.
    //             await foreach (var chunk in _llmService.SendPromptStreamingAsync(prompt).ConfigureAwait(false))
    //             {
    //                 sb.Append(chunk);
    //
    //                 // Update UI safely on the main thread
    //                 MainThread.BeginInvokeOnMainThread(() =>
    //                 {
    //                     assistantMessage.Content += chunk;
    //                 });
    //             }
    //
    //             var finalResponse = sb.ToString();
    //             // This is a good place to do final, non-UI-specific work.
    //             await _conversationMemory.AddAsync(assistantMessage);
    //
    //             // You can also do a final UI update here if needed.
    //             MainThread.BeginInvokeOnMainThread(() =>
    //             {
    //                 assistantMessage.Content = finalResponse;
    //             });
    //         });
    //     }
    //     finally
    //     {
    //         // This will be hit only after the entire Task.Run block is complete.
    //         IsBusy = false;    
    //     }
    // }
    [RelayCommand]
    public async Task SendPromptAsync()
    {
        try
        {
            IsBusy = true;

            var prompt = PromptText?.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            // Create and store user message immediately
            var userPrompt = new Message { Sender = "User", Content = prompt };
            await _conversationMemory.AddAsync(userPrompt);
            await _memoryService.SaveEntryAsync("User", userPrompt.Content, DateTime.UtcNow);

            Messages.Add(userPrompt);
            PromptText = string.Empty;

            // Create assistant placeholder for UI streaming
            var assistantMessage = new Message { Sender = "AI", Content = string.Empty };
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
                var finalAssistantMessage = new Message { Sender = "AI", Content = finalResponse };
                await _conversationMemory.AddAsync(finalAssistantMessage);
                await _memoryService.SaveEntryAsync("AI", finalAssistantMessage.Content, DateTime.UtcNow);

            }));
        }
        finally
        {
            IsBusy = false;
        }
    }


    [RelayCommand]
    public async Task old_SendPromptAsync()
    {
        var prompt = PromptText?.Trim();
        if (string.IsNullOrEmpty(prompt)) return;

        Messages.Add(new Message { Sender = "User", Content = prompt });
        PromptText = string.Empty;

        var assistantMessage = new Message { Sender = "AI", Content = string.Empty };
        Messages.Add(assistantMessage);

        // Run the whole streaming read off the main thread
        var chunks = await Task.Run(async () =>
        {
            var results = new List<string>();
            await foreach (var chunk in _llmService.SendPromptStreamingAsync(prompt).ConfigureAwait(false))
            {
                results.Add(chunk);
            }
            return results;
        });

        // Append chunks to UI-bound property on the main thread
        foreach (var chunk in chunks)
        {
            assistantMessage.Content += chunk;
        }
    }

}