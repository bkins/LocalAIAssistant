using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Knowledge.Journals.Models;
using LocalAIAssistant.Knowledge.Journals.Views;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public partial class JournalDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IJournalApiClientFactory _clientFactory;

    [ObservableProperty] private bool                  _isLoading;
    [ObservableProperty] private string                _text = string.Empty;
    [ObservableProperty] private DateTimeOffset        _createdAt;
    [ObservableProperty] private IReadOnlyList<string> _tags = Array.Empty<string>();
    [ObservableProperty] private string?               _mood;
    [ObservableProperty] private int?                  _moodScore;
    [ObservableProperty] private JournalEntryState     _state;
    [ObservableProperty] private Guid                  _journalId;

    private Exception _caughtException;
    
    public  bool      IsEdited => State == JournalEntryState.Edited && false;
    
    public JournalDetailViewModel(IJournalApiClientFactory clientFactory)
    {
        _clientFactory   = clientFactory;
        _caughtException = new Exception();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) &&
            Guid.TryParse(idObj?.ToString(), out var id))
        {
            _journalId = id;
        }
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
                CreatedAt = entry.CreatedAt;
                Tags      = entry.Tags;
                Mood      = entry.Mood;
                State     = entry.State;
                MoodScore = entry.MoodScore;
            }
        }
        catch (Exception e)
        {
            _caughtException = e;
        }
        finally
        {
            IsLoading = false;
        }
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
