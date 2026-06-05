using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Tasks;
using LocalAIAssistant.Knowledge.Tasks.Models;
using LocalAIAssistant.Knowledge.Tasks.Views;

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
    [ObservableProperty] private bool            _showAsMarkdown;
    [ObservableProperty] private bool            _hasError;
    [ObservableProperty] private string          _errorMessage;
    [ObservableProperty] private string?         _workspace;

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

        if (query.TryGetValue("workspace", out var wsObj) && wsObj?.ToString() is { Length: > 0 } ws)
            Workspace = Uri.UnescapeDataString(ws);
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
        CreatedAt        = item.CreatedAt.LocalDateTime;
        UpdatedAt        = item.UpdatedAt.LocalDateTime;
        DueDate          = item.DueDate?.LocalDateTime;
        CompletedAt      = item.CompletedAt?.LocalDateTime;
        IsCompleted      = item.IsCompleted;
        Tags             = item.Tags.Count > 0
                                   ? string.Join(", ", item.Tags)
                                   : string.Empty;
        SetDtoError(item);
    }
    
    private void SetDtoError( TasksDto? item )
    {
        if (item?.Error is null) return;

        HasError     = true;
        ErrorMessage = item.Error.Message;
    }

    [RelayCommand]
    private async Task EditTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(_id)) return;
        await Shell.Current.GoToAsync($"{nameof(EditTaskPage)}?id={_id}");
    }
}