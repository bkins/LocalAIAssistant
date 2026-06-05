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
    [ObservableProperty] private string                _errorMessage;
    [ObservableProperty] private string?               _workspace;

    private Exception _caughtException;

    [ObservableProperty] private bool _isEdited;

    public ObservableCollection<AttachmentViewModel> Attachments    { get; } = new();
    public bool                                      HasAttachments => Attachments.Count > 0;

    public JournalDetailViewModel( IJournalApiClientFactory  clientFactory
                                 , IMediaAttachmentApiClient mediaClient )
    {
        _clientFactory   = clientFactory;
        _mediaClient     = mediaClient;
        _caughtException = new Exception();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) &&
            Guid.TryParse(idObj?.ToString(), out var id))
        {
            _journalId = id;
        }

        if (query.TryGetValue("workspace", out var wsObj) && wsObj?.ToString() is { Length: > 0 } ws)
            Workspace = Uri.UnescapeDataString(ws);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_journalId == Guid.Empty)
            return;

        IsLoading = true;
        try
        {
            var client = _clientFactory.Create();
            var entry  = await client.GetByIdAsync(_journalId);

            if (entry is not null)
            {
                Text      = entry.Text;
                CreatedAt = entry.CreatedAt.LocalDateTime;
                Tags      = entry.Tags;
                Mood      = entry.Mood;
                State     = entry.State;
                MoodScore = entry.MoodScore;
                IsEdited  = entry.IsEdited;

                SetDtoError(entry);
            }

            await LoadAttachmentsAsync();
        }
        catch (Exception exception)
        {
            _caughtException = exception;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAttachmentsAsync()
    {
        var list = await _mediaClient.ListAsync(_journalId);
        Attachments.Clear();

        if (list is null) return;

        foreach (var attachment in list)
            Attachments.Add(new AttachmentViewModel(attachment, BuildEnvironment.ApiBaseUrl, _ => Task.CompletedTask));

        OnPropertyChanged(nameof(HasAttachments));
    }

    private void SetDtoError( JournalEntryDto? entry )
    {
        if (entry?.Error is null) return;

        HasError     = true;
        ErrorMessage = entry.Error.Message;
    }

    [RelayCommand]
    private async Task ViewRevisionHistoryAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(JournalRevisionHistoryPage)}?id={_journalId}");
    }

    [RelayCommand]
    private async Task EditEntryAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(EditJournalEntryPage)}?id={_journalId}");
    }
}
