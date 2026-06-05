using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Tasks;

namespace LocalAIAssistant.Knowledge.Tasks.ViewModels;

public sealed partial class EditTaskViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITaskApiClientFactory _clientFactory;

    private Guid _taskId;

    public string  ShortDescription { get; set; } = string.Empty;
    public string? Details          { get; set; }
    public string  Tags             { get; set; } = string.Empty;

    [ObservableProperty] private bool _isLoading;

    public EditTaskViewModel(ITaskApiClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj)
         && Guid.TryParse(idObj?.ToString(), out var id))
        {
            _taskId = id;
        }
    }

    public async Task LoadAsync()
    {
        if (_taskId == Guid.Empty) return;

        IsLoading = true;
        try
        {
            var client = _clientFactory.Create();
            var task   = await client.GetByIdAsync(_taskId);

            if (task is not null)
            {
                ShortDescription = task.ShortDescription;
                Details          = task.Details;
                Tags             = task.Tags.Count > 0
                                       ? string.Join(", ", task.Tags)
                                       : string.Empty;
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(string.Empty);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_taskId == Guid.Empty) return;

        var client = _clientFactory.Create();
        await client.EditTaskAsync(_taskId
                                 , ShortDescription
                                 , Details
                                 , ParseTags(Tags));

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
        => await Shell.Current.GoToAsync("..");

    private static IReadOnlyList<string>? ParseTags(string tags)
        => string.IsNullOrWhiteSpace(tags)
               ? null
               : tags.Split(',')
                     .Select(tag => tag.Trim())
                     .Where(tag => tag.Length > 0)
                     .ToList();
}
