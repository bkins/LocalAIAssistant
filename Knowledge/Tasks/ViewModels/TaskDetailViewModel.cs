using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Tasks;
using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LocalAIAssistant.Knowledge.Tasks.ViewModels;

public partial class TaskDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITaskApiClientFactory _clientFactory;

    [ObservableProperty] private bool            _isLoading;
    [ObservableProperty] private int             _position;
    [ObservableProperty] private string          _shortDescription = string.Empty;
    [ObservableProperty] private string?         _details;
    [ObservableProperty] private TaskPriorityDto _priority;
    [ObservableProperty] private bool            _isImportant;
    [ObservableProperty] private bool            _isUrgent;
    [ObservableProperty] private DateTimeOffset  _createdAt;
    [ObservableProperty] private DateTimeOffset  _updatedAt;
    [ObservableProperty] private DateTimeOffset? _dueDate;
    [ObservableProperty] private DateTimeOffset? _completedAt;
    [ObservableProperty] private bool            _isCompleted;
    [ObservableProperty] private string          _tags = string.Empty;
    [ObservableProperty] private string          _id   = string.Empty;

    public TaskDetailViewModel(ITaskApiClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj)
         && idObj?.ToString() is { Length: > 0 } id)
        {
            _id = id;
        }
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_id))
            return;

        IsLoading = true;

        try
        {
            // GetByIdAsync accepts a Guid, so we parse here rather than storing
            // a Guid on the VM — the API id is a string internally but the
            // existing endpoint route constraint is {id:guid}.
            if (!Guid.TryParse(_id, out var guid))
                return;

            var client = _clientFactory.Create();
            var item   = await client.GetByIdAsync(guid, ct);

            if (item is not null)
                ApplyTask(item);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyTask(TasksDto item)
    {
        ShortDescription = item.ShortDescription;
        Details          = item.Details;
        Priority         = item.Priority;
        IsImportant      = item.IsImportant;
        IsUrgent         = item.IsUrgent;
        CreatedAt        = item.CreatedAt;
        UpdatedAt        = item.UpdatedAt;
        DueDate          = item.DueDate;
        CompletedAt      = item.CompletedAt;
        IsCompleted      = item.IsCompleted;
        Tags             = item.Tags.Count > 0
                                   ? string.Join(", ", item.Tags)
                                   : string.Empty;
    }
}