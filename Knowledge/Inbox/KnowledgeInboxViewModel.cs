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

    [ObservableProperty]
    private ObservableCollection<KnowledgeItemGroup> _groupedItems = new();

    [ObservableProperty] private bool           _isLoading;
    [ObservableProperty] private bool           _isOffline;
    [ObservableProperty] private bool           _hasError;
    [ObservableProperty] private string         _errorMessage = string.Empty;
    [ObservableProperty] private KnowledgeItem? _selectedItem;

    // ── Filter chips ─────────────────────────────────────────────────────────

    public ObservableCollection<FilterChip> TypeFilters { get; } = new()
    {
        new FilterChip("All",      "All",     isSelected: true)
      , new FilterChip("Journals", "Journal")
      , new FilterChip("Tasks",    "Task")
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMultipleWorkspaces))]
    private ObservableCollection<FilterChip> _workspaceFilters = new();

    public bool HasMultipleWorkspaces => WorkspaceFilters.Count > 1;

    private KnowledgeKind? _activeTypeFilter;     // null = All
    private string?        _activeWorkspaceFilter; // null = All

    public KnowledgeInboxViewModel(IKnowledgeClientFactory   clientFactory
                                 , IKnowledgeSyncService     syncService
                                 , ILocalKnowledgeStore      localStore
                                 , LocalAiAssistantDbContext db )
    {
        _clientFactory = clientFactory;
        _syncService   = syncService;
        _localStore    = localStore;
        _db            = db;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading    = true;
        IsOffline    = !_syncService.IsOnline;
        HasError     = false;
        ErrorMessage = string.Empty;

        try
        {
            Items.Clear();

            if (_syncService.IsOnline)
                await LoadOnlineAsync();
            else
                await LoadOfflineAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            HasError     = true;
            ErrorMessage = $"Failed to load inbox: {ex.Message}";
        }
        finally
        {
            RebuildWorkspaceFilters();
            RebuildGroups();
            IsLoading = false;
        }
    }

    private async Task LoadOnlineAsync()
    {
        await _syncService.SyncAsync();

        var items = _localStore.List()
                               .OrderByDescending(item => item.CreatedAt);

        foreach (var item in items)
            _items.Add(item);
    }

    private async Task LoadOfflineAsync()
    {
        var pendingItems = await BuildPendingItemsAsync();

        var localItems = _localStore.List();

        var combined = pendingItems.Concat(localItems)
                                   .OrderByDescending(item => item.CreatedAt);

        foreach (var item in combined)
            _items.Add(item);
    }

    private async Task<IReadOnlyList<KnowledgeItem>> BuildPendingItemsAsync()
    {
        var queued = await _db.OfflineQueue
                              .Where(q => q.Status == OfflineQueueStatus.Pending)
                              .OrderByDescending(q => q.CreatedUtc)
                              .ToListAsync();

        return queued.Select(queueItem => new KnowledgeItem
                                          {
                                                  Id             = queueItem.Id
                                                , Kind           = KnowledgeKind.Pending
                                                , Status         = KnowledgeStatus.Active
                                                , Title          = BuildPendingTitle(queueItem.Input)
                                                , CreatedAt      = new DateTimeOffset(queueItem.CreatedUtc, TimeSpan.Zero)
                                                , LastModifiedAt = new DateTimeOffset(queueItem.CreatedUtc, TimeSpan.Zero)
                                                , IsQueued       = true
                                          })
                     .ToList();
    }

    // ── Filter commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectTypeFilter(FilterChip chip)
    {
        foreach (var filterChip in TypeFilters)
            filterChip.IsSelected = filterChip == chip;

        _activeTypeFilter = chip.Value switch
        {
            "Journal" => KnowledgeKind.Journal
          , "Task"    => KnowledgeKind.Task
          , _         => null
        };

        RebuildGroups();
    }

    [RelayCommand]
    private void SelectWorkspaceFilter(FilterChip chip)
    {
        foreach (var filterChip in WorkspaceFilters)
            filterChip.IsSelected = filterChip == chip;

        _activeWorkspaceFilter = chip.Value == "All" ? null : chip.Value;

        RebuildGroups();
    }

    // ── Groups / filtering ────────────────────────────────────────────────────

    private void RebuildGroups()
    {
        var filtered = _items.AsEnumerable();

        if (_activeTypeFilter.HasValue)
            filtered = filtered.Where(item => item.Kind == _activeTypeFilter.Value);

        if (_activeWorkspaceFilter is not null)
            filtered = filtered.Where(item => item.Workspace == _activeWorkspaceFilter);

        var rebuilt = filtered
            .GroupBy(item => item.Kind)
            .OrderByDescending(group => group.Key)
            .Select(group => new KnowledgeItemGroup(group.Key, group.OrderByDescending(item => item.CreatedAt)))
            .ToList();

        GroupedItems = new ObservableCollection<KnowledgeItemGroup>(rebuilt);
    }

    private void RebuildWorkspaceFilters()
    {
        var workspaces = _items.Where(item => item.Workspace != null 
                                           && item.Workspace.HasValue())
                               .Select(item => item.Workspace!)
                               .Distinct()
                               .OrderBy(workspace => workspace)
                               .ToList();

        if (workspaces.Count == 0)
        {
            WorkspaceFilters       = new ObservableCollection<FilterChip>();
            _activeWorkspaceFilter = null;
            return;
        }

        var chips = new List<FilterChip> { new FilterChip("All Workspaces", "All", isSelected: true) };
        chips.AddRange(workspaces.Select(ws => new FilterChip(ws, ws)));
        WorkspaceFilters       = new ObservableCollection<FilterChip>(chips);
        _activeWorkspaceFilter = null;
    }

    // ── Archive ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ArchiveAsync(KnowledgeItem item)
    {
        if (item is null || item.IsQueued)
            return;

        var client = _clientFactory.Create();
        await client.ArchiveAsync(item.Id);

        Items.Remove(item);
        RebuildGroups();
    }

    // ── Open (navigate to detail) ─────────────────────────────────────────────

    partial void OnSelectedItemChanged(KnowledgeItem? value)
    {
        if (value is null)
            return;

        if (value.IsQueued.Not())
            OpenCommand.Execute(value);

        SelectedItem = null;
    }

    [RelayCommand]
    private async Task OpenAsync(KnowledgeItem item)
    {
        switch (item.Kind)
        {
            case KnowledgeKind.Journal:
            {
                var url = $"{nameof(JournalDetailPage)}?id={item.Id}";
                if (!string.IsNullOrEmpty(item.Workspace))
                    url += $"&workspace={Uri.EscapeDataString(item.Workspace)}";
                await Shell.Current.GoToAsync(url);
                break;
            }

            case KnowledgeKind.Task:
            {
                var url = $"{nameof(TaskDetailPage)}?id={item.Id}";
                if (!string.IsNullOrEmpty(item.Workspace))
                    url += $"&workspace={Uri.EscapeDataString(item.Workspace)}";
                await Shell.Current.GoToAsync(url);
                break;
            }

            case KnowledgeKind.Pending:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildPendingTitle(string input)
    {
        const int maxLength = 60;
        var       trimmed   = input.Trim();

        return trimmed.Length <= maxLength
                       ? $"Pending: {trimmed}"
                       : $"Pending: {trimmed[..maxLength]}…";
    }
}
