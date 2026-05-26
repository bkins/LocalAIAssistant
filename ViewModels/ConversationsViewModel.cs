using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Core.ConversationHistory;
using LocalAIAssistant.Data;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.ViewModels;

public partial class ConversationsViewModel : ObservableObject
{
    private readonly IConversationApiClient _conversationClient;
    private readonly ChatViewModel          _chatViewModel;
    private readonly ILoggingService        _log;

    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<ConversationSummaryDto> Conversations { get; } = new();

    public ConversationsViewModel( IConversationApiClient conversationClient
                                 , ChatViewModel          chatViewModel
                                 , ILoggingService        log )
    {
        _conversationClient = conversationClient;
        _chatViewModel      = chatViewModel;
        _log                = log;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;

        try
        {
            var conversations = await _conversationClient.GetAllConversationsAsync(ct);

            Conversations.Clear();
            foreach (var conversation in conversations)
                Conversations.Add(conversation);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Failed to load conversation list");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectConversation(ConversationSummaryDto conversation)
    {
        try
        {
            await _chatViewModel.SwitchConversationAsync(conversation.ConversationId);
            await Shell.Current.GoToAsync("//Chat");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Failed to open conversation");
            await Shell.Current.DisplayAlert("Error", "Could not open that conversation. Please try again.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteConversation(ConversationSummaryDto conversation)
    {
        try
        {
            var deleted = await _conversationClient.DeleteConversationAsync(conversation.ConversationId);
            if (deleted)
                Conversations.Remove(conversation);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Failed to delete conversation");
            await Shell.Current.DisplayAlert("Error", "Could not delete that conversation. Please try again.", "OK");
        }
    }

    [RelayCommand]
    private async Task NewConversation()
    {
        try
        {
            var newId = Guid.NewGuid().ToString();
            Preferences.Set(StringConsts.ActiveConversationIdKey, newId);
            await _chatViewModel.SwitchConversationAsync(newId);
            await Shell.Current.GoToAsync("//Chat");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Failed to start new conversation");
            await Shell.Current.DisplayAlert("Error", "Could not start a new chat. Please try again.", "OK");
        }
    }
}
