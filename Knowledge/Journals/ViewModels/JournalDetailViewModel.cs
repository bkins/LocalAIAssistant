using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Core.Media;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Knowledge.Journals.Models;
using LocalAIAssistant.Knowledge.Journals.Views;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public partial class JournalDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IJournalApiClientFactory  _clientFactory;
    private readonly IMediaAttachmentApiClient _mediaClient;

    [ObservableProperty] private bool                  _isLoading;
    [ObservableProperty] private string                _text = string.Empty;
    [ObservableProperty] private DateTimeOffset        _createdAt;
    [ObservableProperty] private IReadOnlyList<string> _tags = Array.Empty<string>();
    [ObservableProperty] private string?               _mood;
    [ObservableProperty] private int?                  _moodScore;
    [ObservableProperty] private JournalEntryState     _state;
    [ObservableProperty] private Guid                  _journalId;
    [ObservableProperty] private bool                  _showAsMarkdown;
    [ObservableProperty] private bool                  _hasError;
    [ObservableProperty] private string                _errorMessage = string.Empty;
    [ObservableProperty] private string?               _workspace;

    [ObservableProperty] private Exception?            _caughtException;
    [ObservableProperty] private bool                  _isEdited;

    public ObservableCollection<AttachmentViewModel> Attachments    { get; } = new();
    public bool                                      HasAttachments => Attachments.Count > 0;

    public JournalDetailViewModel( IJournalApiClientFactory  clientFactory
                                 , IMediaAttachmentApiClient mediaClient )
    {
        _clientFactory   = clientFactory;
        _mediaClient     = mediaClient;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) &&
            Guid.TryParse(idObj?.ToString(), out var id))
        {
            JournalId = id;
        }

        if (query.TryGetValue("workspace", out var wsObj) && wsObj?.ToString() is { Length: > 0 } ws)
            Workspace = Uri.UnescapeDataString(ws);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        CaughtException = null;
        Text = string.Empty;
        Tags = Array.Empty<string>();
        Mood = null;
        MoodScore = null;
        IsEdited = false;
        CreatedAt = default;
        State = default;
        Attachments.Clear();
        OnPropertyChanged(nameof(HasAttachments));

        if (JournalId == Guid.Empty)
        {
            HasError = true;
            ErrorMessage = "This journal entry has an invalid identifier.";
            return;
        }

        IsLoading = true;
        var journalLoaded = false;
        try
        {
            var client = _clientFactory.Create();
            var entry  = await client.GetByIdAsync(JournalId);

            if (entry is null)
            {
                HasError = true;
                ErrorMessage = "This journal entry could not be found. Return to the Inbox and refresh the list.";
                return;
            }

            if (entry.Error is not null)
            {
                HasError = true;
                ErrorMessage = "Unable to load this journal entry. Check the API connection, then retry.";
                Serilog.Log.Warning("Journal detail request failed for {JournalId}: {ExceptionType}", JournalId, entry.Error.ExceptionType);
                return;
            }

            Text      = entry.Text;
            CreatedAt = entry.CreatedAt.LocalDateTime;
            Tags      = entry.Tags;
            Mood      = entry.Mood;
            State     = entry.State;
            MoodScore = entry.MoodScore;
            IsEdited  = entry.IsEdited;

            journalLoaded = true;
            await LoadAttachmentsAsync();
        }
        catch (Exception exception)
        {
            CaughtException = exception;
            HasError = true;
            ErrorMessage = journalLoaded
                ? "The journal loaded, but its attachments could not be displayed. Retry to reload."
                : "Unable to display this journal entry. Retry to reload; if it continues, check the app logs.";
            Serilog.Log.Error(exception, "Failed to load journal detail {JournalId}", JournalId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAttachmentsAsync()
    {
        var list = await _mediaClient.ListAsync(JournalId);
        Attachments.Clear();

        if (list is null) return;

        foreach (var attachment in list)
            Attachments.Add(new AttachmentViewModel(attachment, BuildEnvironment.ApiBaseUrl, _ => Task.CompletedTask));

        OnPropertyChanged(nameof(HasAttachments));
    }

    [RelayCommand]
    private async Task ViewRevisionHistoryAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(JournalRevisionHistoryPage)}?id={JournalId}");
    }

    [RelayCommand]
    private async Task EditEntryAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(EditJournalEntryPage)}?id={JournalId}");
    }
}
