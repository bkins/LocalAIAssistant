using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Knowledge.Journals.Clients;

namespace LocalAIAssistant.Knowledge.Journals.ViewModels;

public partial class JournalDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IJournalApiClient _client;

    [ObservableProperty] private bool           _isLoading;
    [ObservableProperty] private string         _text = string.Empty;
    [ObservableProperty] private DateTimeOffset _createdAt;

    private Guid _journalId;

    public JournalDetailViewModel(IJournalApiClient client)
    {
        _client = client;
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
            var entry = await _client.GetByIdAsync(_journalId);
            if (entry is not null)
            {
                Text      = entry.Text;
                CreatedAt = entry.CreatedAt;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
