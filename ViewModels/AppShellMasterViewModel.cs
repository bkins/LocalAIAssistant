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
    private ApiEnvironmentDescriptor  _environment;
    
    private readonly EnvironmentHandshakeState       _handshakeState;
    private static   IConnectivityState              _connectivity;
    private readonly ICognitivePlatformClientFactory _cpClientFactory;
    private readonly IOfflineQueueService            _offlineQueueService;
    
    [ObservableProperty] private int _pendingQueueCount;

    public static bool IsOffline => _connectivity.IsOffline;

    // These properties will hold your individual view models
    public ApiHealthViewModel ApiHealthViewModel { get; }
    public AppShellViewModel  AppShellViewModel  { get; }

    public string EnvironmentName => _environment.Name;

    private static   Color                      _statusColor;
    private static   Timer                      _timer;
    private readonly EnvironmentHandshakeResult _currentEnv;

    public Color StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public  ICommand CheckStatusCommand { get; }

    [RelayCommand]
    private async Task CheckApiStatus()
    {
        // var cpClient = _cpClientFactory.Create();
        // await cpClient.Ping();
        
        await ApiHealthViewModel.CheckApiStatusAsync();
        UpdateStatusColor();
    }

    public AppShellMasterViewModel( ApiHealthViewModel              apiHealthViewModel
                                  , AppShellViewModel               appShellViewModel
                                  , ApiEnvironmentDescriptor        environment
                                  , EnvironmentHandshakeState       handshakeState
                                  , IConnectivityState              connectivity
                                  , ICognitivePlatformClientFactory cpClientFactory
                                  , IOfflineQueueService            offlineQueueService )
    {
        ApiHealthViewModel = apiHealthViewModel;
        AppShellViewModel  = appShellViewModel;

        _cpClientFactory = cpClientFactory;
        _statusColor     = Colors.Red;

        _environment = environment;
        _environment.PropertyChanged += ( _, __ ) =>
        {
            OnPropertyChanged(nameof(EnvironmentName));
            UpdateStatusColor();
        };

        _handshakeState = handshakeState;

        _currentEnv = _handshakeState.Current;

        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += ( _, _ ) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(IsOffline));
                UpdateStatusColor();
            });
        };

        CheckStatusCommand = new Command(async void () =>
        {
            try
            {
                await CheckApiStatus();
            }
            catch (Exception e)
            {
                // Gulp!
            }
        });
        _offlineQueueService = offlineQueueService;

        UpdateStatusColor();
        DisplayEnvMismatchMessage();
    }

    public async Task InitializeAsync([CallerMemberName] string memberName = "")
    {
        try
        {
            // var cpClient = _cpClientFactory.Create();
            // await cpClient.Ping(memberName);

            await RefreshQueueCountAsync();

            
        }
        catch
        {
            // ConnectivityState already tracks failure
        }
    }

    private void UpdateStatusColor()
    {
        // Simple rule for now:
        // Offline = red, Online = green.
        // (Later we can incorporate environment-specific colors if you want.)
        StatusColor = IsOffline
                              ? Colors.Red
                              : Colors.Green;
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
}