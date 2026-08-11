using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Core.ConversationHistory;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.ViewModels;

public partial class AgentChatViewModel : ObservableObject
{
    private readonly AgentJobService _agentJobService;

    [ObservableProperty] private ObservableCollection<ConversationMetadataDto> _conversations = new();
    [ObservableProperty] private ConversationMetadataDto? _selectedConversation;
    [ObservableProperty] private ObservableCollection<ConversationTurnDto> _messages = new();
    [ObservableProperty] private string _promptText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = string.Empty;

    public bool HasStatusText => !string.IsNullOrWhiteSpace(StatusText);

    partial void OnStatusTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusText));
    }

    private static readonly ConversationMetadataDto NewThreadPlaceholder = new()
                                                                          {
                                                                              ConversationId = string.Empty
                                                                            , Name           = "(New Conversation Thread)"
                                                                          };

    public AgentChatViewModel(AgentJobService agentJobService)
    {
        _agentJobService = agentJobService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadConversationsAsync();
    }

    public async Task LoadConversationsAsync()
    {
        var list = await _agentJobService.ListConversationsAsync();
        
        // Filter out deleted ones and sort
        var activeList = list.Where(convo => !convo.IsDeleted)
                             .OrderByDescending(convo => convo.LastActiveUtc)
                             .ToList();

        var newConvos = new ObservableCollection<ConversationMetadataDto> { NewThreadPlaceholder };
        foreach (var convo in activeList)
        {
            // Fallback for unnamed threads
            if (string.IsNullOrWhiteSpace(convo.Name))
            {
                convo.Name = $"Thread {convo.ConversationId[..8]}";
            }
            newConvos.Add(convo);
        }

        Conversations = newConvos;

        // Default to New Thread if not set or not in list
        if (SelectedConversation == null || !Conversations.Any(convo => convo.ConversationId == SelectedConversation.ConversationId))
        {
            SelectedConversation = NewThreadPlaceholder;
        }
    }

    // Called when the selected conversation dropdown changes
    partial void OnSelectedConversationChanged(ConversationMetadataDto? value)
    {
        _ = LoadHistoryForConversationAsync(value);
    }

    private async Task LoadHistoryForConversationAsync(ConversationMetadataDto? newValue)
    {
        if (newValue == null || string.IsNullOrEmpty(newValue.ConversationId))
        {
            Messages = new ObservableCollection<ConversationTurnDto>();
            return;
        }

        IsBusy = true;
        StatusText = "Loading history...";
        try
        {
            var history = await _agentJobService.GetHistoryAsync(newValue.ConversationId);
            Messages = new ObservableCollection<ConversationTurnDto>(history);
            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load history: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendPromptAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptText))
            return;

        var prompt = PromptText;
        PromptText = string.Empty;
        IsBusy = true;
        StatusText = "Submitting remote agent job...";

        // Add user turn visually immediately
        var userTurn = new ConversationTurnDto
                       {
                           Role      = "user"
                         , Content   = prompt
                         , Timestamp = DateTimeOffset.UtcNow
                       };
        Messages.Add(userTurn);

        try
        {
            var convoId = string.IsNullOrEmpty(SelectedConversation?.ConversationId) ? null : SelectedConversation.ConversationId;
            var job = await _agentJobService.CreateJobAsync(prompt, convoId);
            var jobId = job.Id;

            StatusText = "Job queued on server. Waiting for workstation poller...";
            
            // Poll the status
            var completed = false;
            var maxAttempts = 60; // 2 minutes timeout
            var attempts = 0;

            while (!completed && attempts < maxAttempts)
            {
                await Task.Delay(2000);
                attempts++;

                var updatedJob = await _agentJobService.GetJobAsync(jobId);
                if (updatedJob == null)
                {
                    StatusText = "Job disappeared from server.";
                    break;
                }

                if (updatedJob.Status == "Running")
                {
                    StatusText = "Antigravity is executing the prompt on workstation...";
                }
                else if (updatedJob.Status == "Completed")
                {
                    completed = true;
                    StatusText = string.Empty;

                    // Add assistant response visually
                    var assistantTurn = new ConversationTurnDto
                                        {
                                            Role      = "assistant"
                                          , Content   = updatedJob.Response ?? "(No response content returned)"
                                          , Timestamp = updatedJob.CompletedUtc ?? DateTimeOffset.UtcNow
                                        };
                    Messages.Add(assistantTurn);

                    // If it was a new thread, refresh thread list and select the new thread
                    if (string.IsNullOrEmpty(convoId) && !string.IsNullOrEmpty(updatedJob.ConversationId))
                    {
                        var createdConvoId = updatedJob.ConversationId;
                        await LoadConversationsAsync();
                        
                        var matchedConvo = Conversations.FirstOrDefault(c => c.ConversationId == createdConvoId);
                        if (matchedConvo != null)
                        {
                            SelectedConversation = matchedConvo;
                        }
                    }
                }
                else if (updatedJob.Status == "Failed")
                {
                    completed = true;
                    StatusText = $"Job failed: {updatedJob.Error}";
                    
                    var errorTurn = new ConversationTurnDto
                                    {
                                        Role      = "assistant"
                                      , Content   = $"[Remote Agent Error] {updatedJob.Error}"
                                      , Timestamp = DateTimeOffset.UtcNow
                                    };
                    Messages.Add(errorTurn);
                }
            }

            if (!completed)
            {
                StatusText = "Timeout: Antigravity did not respond within 2 minutes. The task is still running on the workstation.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error submitting job: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
