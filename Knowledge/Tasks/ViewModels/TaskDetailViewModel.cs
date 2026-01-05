using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Knowledge.Tasks.Clients;

namespace LocalAIAssistant.Knowledge.Tasks.ViewModels;

public partial class TaskDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITaskApiClient _client;
    
    [ObservableProperty] private bool           _isLoading;
    [ObservableProperty] private string         _text = string.Empty;
    [ObservableProperty] private DateTimeOffset _createdAt;

    private Guid _id;

    public TaskDetailViewModel(ITaskApiClient client)
    {
        _client = client;
    }
    
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) &&
            Guid.TryParse(idObj?.ToString(), out var id))
        {
            _id = id;
        }
    }
    
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_id == Guid.Empty)
            return;

        IsLoading = true;
        try
        {
            var item = await _client.GetByIdAsync(_id);
            if (item is not null)
            {
                Text      = item.Text;
                CreatedAt = item.CreatedAt;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}