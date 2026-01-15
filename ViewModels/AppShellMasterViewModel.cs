using CommunityToolkit.Mvvm.ComponentModel;
using CP.Client.Core.Common.ConectivityToApi;
using LocalAIAssistant.CognitivePlatform;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.ViewModels;

public partial class AppShellMasterViewModel : ObservableObject
{
    private ApiEnvironmentService _environment;

    private readonly IConnectivityState       _connectivity;
    private readonly ICognitivePlatformClientFactory _cpClientFactory;

    public bool IsOffline => _connectivity.IsOffline;

    // These properties will hold your individual view models
    public ApiHealthViewModel ApiHealthViewModel { get; }
    public AppShellViewModel  AppShellViewModel  { get; }

    public string EnvironmentName => _environment.Current.ToString();

    private Color _statusColor;
    public Color StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public AppShellMasterViewModel (ApiHealthViewModel              apiHealthViewModel
                                  , AppShellViewModel               appShellViewModel
                                  , ApiEnvironmentService           environment
                                  , IConnectivityState              connectivity
                                  , ICognitivePlatformClientFactory cpClientFactory)
    {
        ApiHealthViewModel = apiHealthViewModel;
        AppShellViewModel  = appShellViewModel;

        _cpClientFactory = cpClientFactory;

        _environment = environment;
        _environment.PropertyChanged += (_
                                       , __) =>
        {
            OnPropertyChanged(nameof(EnvironmentName));
            UpdateStatusColor();
        };

        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(IsOffline));
                UpdateStatusColor();
            });
        };

        UpdateStatusColor();
    }

    public async Task InitializeAsync()
    {
        try
        {
            var cpClient = _cpClientFactory.Create();
            await cpClient.Ping();
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
}