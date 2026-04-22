using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CP.Client.Core.Avails;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Knowledge.Journals.Views;
using LocalAIAssistant.Knowledge.Tasks.Views;
using Microsoft.EntityFrameworkCore;

namespace LocalAIAssistant.Knowledge.Inbox;

public partial class KnowledgeInboxViewModel : ObservableObject
{
    private readonly IKnowledgeClientFactory   _clientFactory;
    private readonly IKnowledgeSyncService     _syncService;
    private readonly ILocalKnowledgeStore      _localStore;
    private readonly LocalAiAssistantDbContext _db;

    public ObservableCollection<KnowledgeItem> Items => _items;
    private readonly ObservableCollection<KnowledgeItem> _items = new();

    [ObservableProperty] private bool           _isLoading;
    [ObservableProperty] private bool           _isOffline;
    [ObservableProperty] private KnowledgeItem? _selectedItem;
    
    //TODO: Implement later
    // public string KindDisplay => Kind.ToString();

    private Exception _caughtException;
    
    public KnowledgeInboxViewModel(IKnowledgeClientFactory   clientFactory
                                 , IKnowledgeSyncService     syncService
                                 , ILocalKnowledgeStore      localStore
                                 , LocalAiAssistantDbContext db )
    {
        _clientFactory   = clientFactory;
        _syncService     = syncService;
        _localStore      = localStore;
        _db              = db;
        _caughtException = new Exception();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        
        if (IsLoading) return;

        IsLoading = true;
        IsOffline = !_syncService.IsOnline;
        
        try
        {
            Items.Clear();
            
            if (_syncService.IsOnline)
                await LoadOnlineAsync();
            else
                await LoadOfflineAsync();
            
            // var client = _clientFactory.Create();
            // var items  = await client.GetKnowledgeAsync();
            //
            // foreach (var item in items)
            // {
            //     Items.Add(item);
            // }
        }
        catch(Exception e)
        {
            _caughtException = e;
        }
        finally
        {
            IsLoading = false;
        }
    }
    private async Task LoadOnlineAsync()
    {
        await _syncService.SyncAsync();

        var client = _clientFactory.Create();
        var items  = await client.GetKnowledgeAsync();

        foreach (var item in items)
            _items.Add(item);
    }

    private async Task LoadOfflineAsync()
    {
        var pendingItems = await BuildPendingItemsAsync();

        foreach (var item in pendingItems)
            _items.Add(item);
        
        var localItems = _localStore.List();

        foreach (var item in localItems)
            _items.Add(item);

    }
    
    private async Task<IReadOnlyList<KnowledgeItem>> BuildPendingItemsAsync()
    {
        var queued = await _db.OfflineQueue
                              .Where(q => q.Status == OfflineQueueStatus.Pending)
                              .OrderBy(q => q.CreatedUtc)
                              .ToListAsync();

        return queued
               .Select(q => new KnowledgeItem
                            {
                                    Id             = q.Id
                                  , Kind           = KnowledgeKind.Pending
                                  , Status         = KnowledgeStatus.Active
                                  , Title          = BuildPendingTitle(q.Input)
                                  , CreatedAt      = new DateTimeOffset(q.CreatedUtc, TimeSpan.Zero)
                                  , LastModifiedAt = new DateTimeOffset(q.CreatedUtc, TimeSpan.Zero)
                                  , IsQueued       = true
                            })
               .ToList();
    }

    private static string BuildPendingTitle(string input)
    {
        const int maxLength = 60;
        var       trimmed   = input.Trim();

        return trimmed.Length <= maxLength
                       ? $"Pending: {trimmed}"
                       : $"Pending: {trimmed[..maxLength]}…";
    }
    
    partial void OnSelectedItemChanged(KnowledgeItem? value)
    {
        if (value is null)
            return;
        
        if (value.IsQueued.Not())
            OpenCommand.Execute(value);
        
        // OpenCommand.Execute(value);

        // Allow re-tapping the same item later
        SelectedItem = null;
    }
    [RelayCommand]
    private async Task ArchiveAsync(KnowledgeItem item)
    {
        if (item is null || item.IsQueued)
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
          
            case KnowledgeKind.Pending:
                // Queued items are not navigable yet
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
