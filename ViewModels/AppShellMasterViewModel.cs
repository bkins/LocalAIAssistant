using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Core.Environment.Models;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.ViewModels;

public partial class AppShellMasterViewModel : ObservableObject
{
    private ApiEnvironmentDescriptor _environment;

    private readonly EnvironmentHandshakeState       _handshakeState;
    private static   IConnectivityState              _connectivity;
    private readonly ICognitivePlatformClientFactory _cpClientFactory;
    private readonly IOfflineQueueService            _offlineQueueService;

    [ObservableProperty] private int _pendingQueueCount;

    public static bool IsOffline => _connectivity.IsOffline;

    public ApiHealthViewModel ApiHealthViewModel { get; }
    public AppShellViewModel  AppShellViewModel  { get; }
    public UsageViewModel     UsageViewModel     { get; }

    public string EnvironmentName => _environment.Name;

    private static Color _statusColor;
    private static Timer _timer;
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
                                  , IOfflineQueueService            offlineQueueService )
    {
        ApiHealthViewModel = apiHealthViewModel;
        AppShellViewModel  = appShellViewModel;
        UsageViewModel     = usageViewModel;

        _cpClientFactory = cpClientFactory;
        _statusColor     = Colors.Red;

        _environment = environment;
        _environment.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(EnvironmentName));
            UpdateStatusColor();
        };

        _handshakeState = handshakeState;
        _currentEnv     = _handshakeState.Current;

        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(IsOffline));
                UpdateStatusColor();
            });
        };

        CheckStatusCommand = new Command(async void () =>
        {
            try { await CheckApiStatus(); }
            catch { /* ConnectivityState tracks failure */ }
        });

        _offlineQueueService = offlineQueueService;

        UpdateStatusColor();
        DisplayEnvMismatchMessage();
    }

    public async Task InitializeAsync([CallerMemberName] string memberName = "")
    {
        try
        {
            await RefreshQueueCountAsync();
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

    public string TimeSinceLastCheck => ApiHealthViewModel.TimeSinceLastCheck;
}