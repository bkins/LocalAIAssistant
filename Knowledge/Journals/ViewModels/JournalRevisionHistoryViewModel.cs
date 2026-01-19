using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public sealed partial class JournalRevisionHistoryViewModel : ObservableObject, IQueryAttributable
{
    private readonly IJournalApiClientFactory _clientFactory;

    [ObservableProperty] private bool _isLoading;

    public bool HasBeenEdited      => Revisions.Count > 1;
    public bool HasNeverBeenEdited => Revisions.Count == 1;

    
    public IReadOnlyList<JournalRevisionDto> Revisions { get; private set; } = Array.Empty<JournalRevisionDto>();

    public bool HasRevisions => Revisions.Count > 0;

    private Guid _journalId;

    public JournalRevisionHistoryViewModel(IJournalApiClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj)
         && Guid.TryParse(idObj?.ToString(), out var id))
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
            
            Revisions = await client.GetRevisionsAsync(_journalId)
                     ?? Array.Empty<JournalRevisionDto>();

            OnPropertyChanged(nameof(Revisions));
            OnPropertyChanged(nameof(HasRevisions));
            OnPropertyChanged(nameof(HasBeenEdited));
            OnPropertyChanged(nameof(HasNeverBeenEdited));
        }
        finally
        {
            IsLoading = false;
        }
    }
}

