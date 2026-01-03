using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Interfaces;

public partial class ConversationViewModel : ObservableObject
{
    private readonly IOrchestratorService _orchestrator;

    [ObservableProperty] private string _userInput = "";
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _currentAssistantLine = ""; // for live streaming into a single bubble

    public ObservableCollection<(string role, string text)> Messages { get; } = new();

    // you can bind this from your Persona picker or use IPersonalityService.Current
    public Personality SelectedPersonality { get; set; }

    public ConversationViewModel(IOrchestratorService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput) 
         || SelectedPersonality == null) return;

        IsBusy = true;
        var input = UserInput;
        UserInput = "";

        Messages.Add((Senders.User, input));
        CurrentAssistantLine = "";

        try
        {
            // Was calling:
            // await foreach (var chunk in _orchestrator.ProcessAsync(input, SelectedPersonality))
            //Now calling this to 
            await foreach (var chunk in _orchestrator.HandleUserMessageStreamingAsync(input))
            {
                // display streaming tokens live
                CurrentAssistantLine += chunk;
            }
            // finalize the bubble at the end
            Messages.Add((Senders.Assistant, CurrentAssistantLine));
            CurrentAssistantLine = "";
        }
        catch (Exception ex)
        {
            Messages.Add((Senders.Assistant, $"[Error] {ex.Message}"));
        }
        finally
        {
            IsBusy = false;
        }
    }
}