using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;
using LocalAIAssistant.Knowledge.Journals.Views;
using LocalAIAssistant.Knowledge.Tasks.Views;

namespace LocalAIAssistant.Knowledge.Inbox;

public partial class KnowledgeInboxViewModel : ObservableObject
{
    private readonly IKnowledgeClientFactory _clientFactory;

    public ObservableCollection<KnowledgeItem> Items => _items;
    private readonly ObservableCollection<KnowledgeItem> _items = new();

    [ObservableProperty] private bool           _isLoading;
    [ObservableProperty] private KnowledgeItem? _selectedItem;
    
    //TODO: Implement later
    // public string KindDisplay => Kind.ToString();

    public KnowledgeInboxViewModel(IKnowledgeClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            Items.Clear();
            var client = _clientFactory.Create();
            var items  = await client.GetKnowledgeAsync();
            
            foreach (var item in items)
            {
                Items.Add(item);
            }
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
        var client = _clientFactory.Create();
        await client.ArchiveAsync(item.Id);

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
