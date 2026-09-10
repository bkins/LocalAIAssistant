using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.MemoryReview;
using LocalAIAssistant.CognitivePlatform.CpClients.Personas;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using Message = LocalAIAssistant.Data.Models.Message;

namespace LocalAIAssistant.ViewModels;

public partial class MemoryManagementViewModel : ObservableObject, IDisposable
{
    private readonly IConversationMemory     _conversationMemory;
    private readonly IMemoryReviewApiClient   _memoryReviewApiClient;
    private readonly IMemoryConfirmationApiClient _memoryConfirmationApiClient;
    private readonly AppShellMasterViewModel _appShellMasterViewModel;
    private readonly System.ComponentModel.PropertyChangedEventHandler _appShellPropertyChangedHandler;

    [ObservableProperty]
    private ObservableCollection<Message> _shortTermMessages = new();

    [ObservableProperty]
    private ObservableCollection<Message> _longTermMessages = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowShortTerm))]
    [NotifyPropertyChangedFor(nameof(ShowLongTerm))]
    [NotifyPropertyChangedFor(nameof(IsShortTermSelected))]
    [NotifyPropertyChangedFor(nameof(IsLongTermSelected))]
    private int _selectedTabIndex = 0; // 0 = Short-Term, 1 = Long-Term

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowShortTerm))]
    [NotifyPropertyChangedFor(nameof(ShowLongTerm))]
    private bool _isMobileLayout = DeviceInfo.Idiom == DeviceIdiom.Phone;

    public bool IsShortTermSelected => SelectedTabIndex == 0;
    public bool IsLongTermSelected  => SelectedTabIndex == 1;

    public bool ShowShortTerm => !IsMobileLayout || SelectedTabIndex == 0;
    public bool ShowLongTerm  => !IsMobileLayout || SelectedTabIndex == 1;

    public int ShortTermCount => ShortTermMessages.Count;
    public int LongTermCount  => LongTermMessages.Count;

    public int PendingMemoryConfirmationCount => _appShellMasterViewModel.PendingMemoryConfirmationCount;
    public bool HasPendingMemoryConfirmation  => PendingMemoryConfirmationCount > 0;

    [ObservableProperty]
    private bool _isProvisionalReviewVisible;

    [ObservableProperty]
    private ObservableCollection<MemoryReviewItemDto> _memoryReviewItems = new();

    [ObservableProperty]
    private ObservableCollection<MemoryReviewSourceDto> _unavailableMemoryReviewSources = new();

    [ObservableProperty]
    private bool _isMemoryReviewAvailable;

    [ObservableProperty]
    private int _globalPendingMemoryConfirmationCount;

    [ObservableProperty]
    private bool _isGlobalPendingMemoryConfirmationCountAvailable;

    [RelayCommand]
    public void ToggleProvisionalReview()
    {
        IsProvisionalReviewVisible = !IsProvisionalReviewVisible;
    }

    [RelayCommand]
    public async Task ConfirmAllProvisionalMemoriesAsync()
    {
        IsProvisionalReviewVisible = false;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task DismissAllProvisionalMemoriesAsync()
    {
        IsProvisionalReviewVisible = false;
        await LoadAsync();
    }

    [RelayCommand]
    public void SelectTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var idx))
        {
            SelectedTabIndex = idx;
        }
    }
    
    public MemoryManagementViewModel( IConversationMemory     conversationMemory
                                    , IMemoryReviewApiClient   memoryReviewApiClient
                                    , IMemoryConfirmationApiClient memoryConfirmationApiClient
                                    , AppShellMasterViewModel appShellMasterViewModel )
    {
        _conversationMemory      = conversationMemory;
        _memoryReviewApiClient   = memoryReviewApiClient;
        _memoryConfirmationApiClient = memoryConfirmationApiClient;
        _appShellMasterViewModel = appShellMasterViewModel;

        _appShellPropertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(AppShellMasterViewModel.PendingMemoryConfirmationCount))
            {
                OnPropertyChanged(nameof(PendingMemoryConfirmationCount));
                OnPropertyChanged(nameof(HasPendingMemoryConfirmation));
            }
        };

        _appShellMasterViewModel.PropertyChanged += _appShellPropertyChangedHandler;
    }

    public void Dispose()
    {
        _appShellMasterViewModel.PropertyChanged -= _appShellPropertyChangedHandler;
    }
    
    [RelayCommand]
    public async Task LoadAsync()
    {
        await _appShellMasterViewModel.RefreshPendingMemoryConfirmationCountAsync();
        await LoadMemoryReviewAsync();
        await LoadGlobalPendingMemoryConfirmationCountAsync();
        OnPropertyChanged(nameof(PendingMemoryConfirmationCount));
        OnPropertyChanged(nameof(HasPendingMemoryConfirmation));

        ShortTermMessages.Clear();
        LongTermMessages.Clear();

        var shortTerm = await _conversationMemory.LoadShortTermAsync();
        foreach (var msg in shortTerm)
            ShortTermMessages.Add(msg);

        var longTerm = await _conversationMemory.LoadLongTermAsync();
        foreach (var msg in longTerm)
            LongTermMessages.Add(msg);
        
        OnPropertyChanged(nameof(ShortTermCount));
        OnPropertyChanged(nameof(LongTermCount));
    }

    [RelayCommand]
    public Task ClearSessionAsync()
    {
        return _conversationMemory.ClearAsync();
    }
    [RelayCommand]
    public async Task ClearShortTermAsync()
    {
        await _conversationMemory.ClearShortTermAsync();
        await LoadAsync();
    }

    [RelayCommand]
    public async Task ClearLongTermAsync()
    {
        await _conversationMemory.ClearLongTermAsync();
        await LoadAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
    }

    private async Task LoadMemoryReviewAsync()
    {
        var review = await _memoryReviewApiClient.GetReviewAsync();

        MemoryReviewItems.Clear();
        UnavailableMemoryReviewSources.Clear();
        IsMemoryReviewAvailable = review is not null;

        if (review is null)
            return;

        foreach (var item in review.Items)
            MemoryReviewItems.Add(item);

        foreach (var source in review.Sources.Where(source => !source.IsAvailable))
            UnavailableMemoryReviewSources.Add(source);
    }

    private async Task LoadGlobalPendingMemoryConfirmationCountAsync()
    {
        var summary = await _memoryConfirmationApiClient.GetSummaryAsync();
        IsGlobalPendingMemoryConfirmationCountAvailable = summary.IsAuthoritative;

        if (summary.IsAuthoritative)
            GlobalPendingMemoryConfirmationCount = summary.PendingCount;
    }

}
