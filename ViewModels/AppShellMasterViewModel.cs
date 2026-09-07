using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.CognitivePlatform.CpClients.Personas;
using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Core.Environment.Models;
using LocalAIAssistant.Data;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.ViewModels;

public partial class AppShellMasterViewModel : ObservableObject, IDisposable
{
    private ApiEnvironmentDescriptor _environment;

    private readonly EnvironmentHandshakeState                                         _handshakeState;
    private static   IConnectivityState                                                _connectivity;
    private readonly ICognitivePlatformClientFactory                                   _cpClientFactory;
    private readonly IMemoryConfirmationApiClient                                      _memoryConfirmationApiClient;
    private readonly IOfflineQueueService                                              _offlineQueueService;
    private readonly System.ComponentModel.PropertyChangedEventHandler?                _environmentPropertyChangedHandler;
    private readonly EventHandler<CP.Client.Core.Common.ConnectivityToApi.ConnectivityStatus>? _connectivityChangedHandler;
    private readonly EventHandler<global::LocalAIAssistant.Services.Interfaces.QueueProcessedEventArgs>? _queueProcessedHandler;

    [ObservableProperty] private int _pendingQueueCount;

    // Pending persona-memory confirmations for the currently active conversation.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MemoryTabTitle))]
    [NotifyPropertyChangedFor(nameof(HasPendingMemoryConfirmation))]
    [NotifyPropertyChangedFor(nameof(HasStalePendingMemoryConfirmation))]
    [NotifyPropertyChangedFor(nameof(MemoryBadgeAccessibilityText))]
    private int _pendingMemoryConfirmationCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStalePendingMemoryConfirmation))]
    [NotifyPropertyChangedFor(nameof(MemoryBadgeAccessibilityText))]
    private bool _isPendingMemoryConfirmationCountStale;

    private string _memoryConfirmationConversationId = string.Empty;

    public string MemoryTabTitle => PendingMemoryConfirmationCount > 0
        ? $"Memory ({PendingMemoryConfirmationCount})"
        : "Memory";

    public bool HasPendingMemoryConfirmation => PendingMemoryConfirmationCount > 0;
    public bool HasStalePendingMemoryConfirmation => HasPendingMemoryConfirmation && IsPendingMemoryConfirmationCountStale;
    public string MemoryBadgeAccessibilityText => HasStalePendingMemoryConfirmation
        ? $"{PendingMemoryConfirmationCount} pending memory confirmations. Count may be stale while offline."
        : $"{PendingMemoryConfirmationCount} pending memory confirmations.";


    public static bool IsOffline => _connectivity.IsOffline;

    public ApiHealthViewModel ApiHealthViewModel { get; }
    public AppShellViewModel  AppShellViewModel  { get; }
    public UsageViewModel     UsageViewModel     { get; }

    public string EnvironmentName => _environment.Name;

    private static Color _statusColor;
    private readonly EnvironmentHandshakeResult _currentEnv;

    public Color StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public ICommand CheckStatusCommand { get; }

    [RelayCommand]
    private async Task CheckApiStatus()
    {
        await ApiHealthViewModel.CheckApiStatusAsync();
        UpdateStatusColor();
    }

    public AppShellMasterViewModel( ApiHealthViewModel              apiHealthViewModel
                                  , AppShellViewModel               appShellViewModel
                                  , UsageViewModel                  usageViewModel
                                  , ApiEnvironmentDescriptor        environment
                                  , EnvironmentHandshakeState       handshakeState
                                  , IConnectivityState              connectivity
                                  , ICognitivePlatformClientFactory cpClientFactory
                                  , IMemoryConfirmationApiClient    memoryConfirmationApiClient
                                  , IOfflineQueueService            offlineQueueService )
    {
        ApiHealthViewModel = apiHealthViewModel;
        AppShellViewModel  = appShellViewModel;
        UsageViewModel     = usageViewModel;

        _cpClientFactory = cpClientFactory;
        _memoryConfirmationApiClient = memoryConfirmationApiClient;
        _statusColor     = Colors.Red;

        _environment = environment;
        _environmentPropertyChangedHandler = (_, __) =>
        {
            OnPropertyChanged(nameof(EnvironmentName));
            UpdateStatusColor();
        };
        _environment.PropertyChanged += _environmentPropertyChangedHandler;

        _handshakeState = handshakeState;
        _currentEnv     = _handshakeState.Current;

        _connectivity = connectivity;
        _connectivityChangedHandler = (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(IsOffline));
                UpdateStatusColor();
            });
        };
        _connectivity.ConnectivityChanged += _connectivityChangedHandler;

        CheckStatusCommand = new Command(async void () =>
        {
            try { await CheckApiStatus(); }
            catch { /* ConnectivityState tracks failure */ }
        });

        _offlineQueueService = offlineQueueService;
        _queueProcessedHandler = async (_, _) =>
        {
            await RefreshQueueCountAsync();
        };
        _offlineQueueService.QueueProcessed += _queueProcessedHandler;

        UpdateStatusColor();
        DisplayEnvMismatchMessage();
    }

    public async Task InitializeAsync([CallerMemberName] string memberName = "")
    {
        try
        {
            await RefreshQueueCountAsync();
            RestoreMemoryConfirmationState();
            await RefreshPendingMemoryConfirmationCountAsync();
        }
        catch
        {
            // ConnectivityState already tracks failure
        }
    }

    /// <summary>
    /// Called by ChatViewModel (or OrchestratorService) after each
    /// completed conversation turn so usage data stays fresh.
    /// </summary>
    public async Task OnConversationTurnCompletedAsync()
    {
        await UsageViewModel.RefreshAfterTurnAsync();
    }

    private void UpdateStatusColor()
    {
        StatusColor = IsOffline ? Colors.Red : Colors.Green;
    }

    private void DisplayEnvMismatchMessage()
    {
        if (_currentEnv.HasMismatch)
        {
            var messageToUser = _currentEnv.UserMessage;
            // TODO: Determine way to display message
        }
    }

    public async Task RefreshQueueCountAsync()
    {
        PendingQueueCount = await _offlineQueueService.GetPendingCountAsync();
    }

    public async Task ActivateMemoryConfirmationConversationAsync(string conversationId)
    {
        RestoreMemoryConfirmationState(conversationId);
        await RefreshPendingMemoryConfirmationCountAsync(conversationId);
    }

    public async Task RefreshPendingMemoryConfirmationCountAsync(string? conversationId = null)
    {
        var activeConversationId = conversationId ?? Preferences.Default.Get(StringConsts.ActiveConversationIdKey, string.Empty);
        if (string.IsNullOrWhiteSpace(activeConversationId)) return;

        if (!string.Equals(_memoryConfirmationConversationId, activeConversationId, StringComparison.Ordinal))
        {
            RestoreMemoryConfirmationState(activeConversationId);
        }

        try
        {
            var refreshResult = await _memoryConfirmationApiClient.RefreshAsync(activeConversationId);
            if (!string.Equals(_memoryConfirmationConversationId, activeConversationId, StringComparison.Ordinal))
                return;

            if (refreshResult.IsAuthoritative)
            {
                UpdatePendingMemoryConfirmationCount(refreshResult.PendingCount, activeConversationId);
            }
            else if (HasPendingMemoryConfirmation)
            {
                IsPendingMemoryConfirmationCountStale = true;
            }
        }
        catch
        {
            if (HasPendingMemoryConfirmation)
            {
                IsPendingMemoryConfirmationCountStale = true;
            }
        }
    }

    public void UpdatePendingMemoryConfirmationCount(int pendingCount, string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;

        _memoryConfirmationConversationId = conversationId;
        PendingMemoryConfirmationCount = Math.Max(0, pendingCount);
        IsPendingMemoryConfirmationCountStale = false;

        Preferences.Default.Set(StringConsts.PendingMemoryConfirmationConversationIdPrefKey, conversationId);
        Preferences.Default.Set(StringConsts.PendingMemoryConfirmationCountPrefKey, PendingMemoryConfirmationCount);
        Preferences.Default.Set(StringConsts.PendingMemoryConfirmationUpdatedUtcPrefKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private void RestoreMemoryConfirmationState(string? conversationId = null)
    {
        var activeConversationId = conversationId ?? Preferences.Default.Get(StringConsts.ActiveConversationIdKey, string.Empty);
        if (string.IsNullOrWhiteSpace(activeConversationId)) return;

        var cachedConversationId = Preferences.Default.Get(StringConsts.PendingMemoryConfirmationConversationIdPrefKey, string.Empty);
        _memoryConfirmationConversationId = activeConversationId;
        if (string.Equals(cachedConversationId, activeConversationId, StringComparison.Ordinal))
        {
            PendingMemoryConfirmationCount = Math.Max(0, Preferences.Default.Get(StringConsts.PendingMemoryConfirmationCountPrefKey, 0));
            IsPendingMemoryConfirmationCountStale = HasPendingMemoryConfirmation;
            return;
        }

        PendingMemoryConfirmationCount = 0;
        IsPendingMemoryConfirmationCountStale = false;
    }

    [RelayCommand]
    private async Task GoToMemory()
    {
        try { await Shell.Current.GoToAsync("//Memory"); }
        catch { /* navigation is best-effort from the title bar */ }
    }

    public string TimeSinceLastCheck => ApiHealthViewModel.TimeSinceLastCheck;

    public void Dispose()
    {
        if (_environment is not null && _environmentPropertyChangedHandler is not null)
        {
            _environment.PropertyChanged -= _environmentPropertyChangedHandler;
        }

        if (_connectivity is not null && _connectivityChangedHandler is not null)
        {
            _connectivity.ConnectivityChanged -= _connectivityChangedHandler;
        }

        if (_offlineQueueService is not null && _queueProcessedHandler is not null)
        {
            _offlineQueueService.QueueProcessed -= _queueProcessedHandler;
        }
    }
}
