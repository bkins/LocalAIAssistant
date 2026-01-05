using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Knowledge.Clients;
using LocalAIAssistant.Knowledge.Journals.Views;
using LocalAIAssistant.Knowledge.Tasks.Views;

namespace LocalAIAssistant.Knowledge.Inbox;

public partial class KnowledgeInboxViewModel : ObservableObject
{
    private readonly IKnowledgeApiClient _client;

    public ObservableCollection<KnowledgeItem> Items => _items;
    private readonly ObservableCollection<KnowledgeItem> _items = new();


    [ObservableProperty] private bool           _isLoading;
    [ObservableProperty] private KnowledgeItem? _selectedItem;
    
    //TODO: Implement later
    // public string KindDisplay => Kind.ToString();

    public KnowledgeInboxViewModel(IKnowledgeApiClient client)
    {
        _client = client;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            Items.Clear();
            
            var items = await _client.GetKnowledgeAsync();
            
            foreach (var item in items)
                Items.Add(item);
        }
        finally
        {
            IsLoading = false;
        }
    }

    
    partial void OnSelectedItemChanged(KnowledgeItem? value)
    {
        if (value is null)
            return;

        OpenCommand.Execute(value);

        // Allow re-tapping the same item later
        SelectedItem = null;
    }
    [RelayCommand]
    private async Task ArchiveAsync(KnowledgeItem item)
    {
        if (item is null)
            return;

        await _client.ArchiveAsync(item.Id);

        // Optimistic UI update
        Items.Remove(item);
    }

    
    [RelayCommand]
    private async Task OpenAsync(KnowledgeItem item)
    {
        switch (item.Kind)
        {
            case KnowledgeKind.Journal:
                await Shell.Current.GoToAsync($"{nameof(JournalDetailPage)}?id={item.Id}");
                break;
            
            case KnowledgeKind.Task:
                await Shell.Current.GoToAsync($"{nameof(TaskDetailPage)}?id={item.Id}");
                break;
            
            // TODO :
            //case KnowledgeKind.<other kind>:
            //    await Shell.Current.GoToAsync($"{nameof(<other kind>)}?id={item.Id}");
            //    break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

}
