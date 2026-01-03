using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Knowledge;

namespace LocalAIAssistant.ViewModels;

public partial class KnowledgeInboxViewModel : ObservableObject
{
    private readonly IKnowledgeApiClient _client;

    public ObservableCollection<KnowledgeItem> Items => _items;
    private readonly ObservableCollection<KnowledgeItem> _items = new();


    [ObservableProperty]
    private bool _isLoading;

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
}
