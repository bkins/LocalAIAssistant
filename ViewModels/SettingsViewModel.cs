
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.CognitivePlatform.CpClients.Coco;
using LocalAIAssistant.Services.Google;
using LocalAIAssistant.Core.Tts;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Health;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Core.Notifications;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly OllamaConfigService      _configService;
    private readonly IPersonalityService      _personalityService;
    private readonly ApiEnvironmentDescriptor _apiEnvironment;
    private readonly AppShellMasterViewModel  _appShellMasterViewModel;
    private readonly IHealthConnectManager?   _healthConnect;
    private readonly ICocoApiClientFactory?   _cocoFactory;
    private readonly IGoogleCalendarService   _googleCalendar;
    private readonly HealthPushService?       _healthPushService;
    private readonly INotificationScheduler?  _notificationScheduler;
    private readonly ILoggingService?         _loggingService;
    
    [ObservableProperty] private string _model;
    [ObservableProperty] private string _endpoint;
    [ObservableProperty] private int    _numPredict;
    [ObservableProperty] private float  _temperature;
    [ObservableProperty] private string _environment;
    [ObservableProperty] private bool   _enableStartupProbes;
    [ObservableProperty] private bool   _enableStartupDiagnostics;
    [ObservableProperty] private bool   _streamingEnabled;

    // ── App Theme ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _selectedTheme = "System";

    public IReadOnlyList<string> AvailableThemes { get; } = new[]
    {
        "System"
      , "Dark"
      , "Light"
    };

    partial void OnSelectedThemeChanged(string value)
    {
        if (value.HasNoValue()) return;
        Preferences.Default.Set("AppThemePreference", value);
        ApplyTheme(value);
    }

    public static void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.UserAppTheme = theme switch
            {
                "Dark"  => AppTheme.Dark,
                "Light" => AppTheme.Light,
                _       => AppTheme.Unspecified
            };
        });
    }

    // ── TTS ───────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAzureSelected))]
    [NotifyPropertyChangedFor(nameof(IsElevenLabsSelected))]
    private string _selectedTtsProvider;

    [ObservableProperty] private string _ttsAzureKey;
    [ObservableProperty] private string _ttsAzureRegion;
    [ObservableProperty] private string _ttsElevenLabsKey;

    public IReadOnlyList<string> TtsProviders { get; } = new[]
    {
        TtsProvider.Maui
      , TtsProvider.Azure
      , TtsProvider.ElevenLabs
    };

    public bool IsAzureSelected      => SelectedTtsProvider == TtsProvider.Azure;
    public bool IsElevenLabsSelected  => SelectedTtsProvider == TtsProvider.ElevenLabs;

    // ── Health Connect (Android-only) ─────────────────────────────────────────
    [ObservableProperty] private string _healthStatusText = "Checking…";

    public bool IsHealthConnectAvailable => _healthConnect is not null;

    // ── Google Calendar ───────────────────────────────────────────────────────
    [ObservableProperty] private string _googleCalendarClientId = string.Empty;
    [ObservableProperty] private string _googleCalendarStatusText = "Not connected";

    // ── Coco (Code Intelligence — Windows only) ───────────────────────────────
    [ObservableProperty] private string _cocoBaseUrl    = StringConsts.CocoDefaultBaseUrl;
    [ObservableProperty] private bool   _cocoEnabled;
    [ObservableProperty] private string _cocoProjectPath            = string.Empty;
    [ObservableProperty] private string _cocoStatusText             = "Not checked";
    [ObservableProperty] private bool   _isIndexing;
    [ObservableProperty] private bool   _cocoClipboardMonitorEnabled = true;
    [ObservableProperty] private string _cocoHotkey                  = StringConsts.CocoDefaultHotkey;

    public bool IsCocoSectionVisible => DeviceInfo.Current.Platform == DevicePlatform.WinUI;

    private string _selectedEnvironment;
    public string SelectedEnvironment
    {
        get => _selectedEnvironment;
        set
        {
            if (_selectedEnvironment == value)
                return;

            _selectedEnvironment = value;
            OnPropertyChanged();

            // _ = ChangeEnvironmentAsync(value);
        }
    }

    public SettingsViewModel (OllamaConfigService      configService
                            , IPersonalityService      personalityService
                            , ApiEnvironmentDescriptor apiEnvironment
                            , AppShellMasterViewModel  appShellMasterViewModel
                            , IGoogleCalendarService   googleCalendar
                            , IServiceProvider         services)
    {
        _apiEnvironment = apiEnvironment;

        Environment = BuildEnvironment.Name;

        SelectedEnvironment = _apiEnvironment.Name;
        _configService       = configService;
        _personalityService  = personalityService;

        // Load the current config
        var cfg = _configService.GetConfig();

        Model       = cfg.Model;
        NumPredict  = cfg.NumPredict;
        Temperature = cfg.Temperature;
        Endpoint    = cfg.Host;

        _appShellMasterViewModel = appShellMasterViewModel;

        _healthConnect               = services.GetService<IHealthConnectManager>();
        _healthPushService           = services.GetService<HealthPushService>();
        _notificationScheduler       = services.GetService<INotificationScheduler>();
        _loggingService              = services.GetService<ILoggingService>();
        if (_healthConnect is not null)
            _ = RefreshHealthStatus();

        SelectedTtsProvider  = Preferences.Default.Get(StringConsts.TtsProviderPrefKey,      TtsProvider.Maui);
        TtsAzureKey          = Preferences.Default.Get(StringConsts.TtsAzureKeyPrefKey,      string.Empty);
        TtsAzureRegion       = Preferences.Default.Get(StringConsts.TtsAzureRegionPrefKey,   "eastus");
        TtsElevenLabsKey     = Preferences.Default.Get(StringConsts.TtsElevenLabsKeyPrefKey, string.Empty);

        _googleCalendar              = googleCalendar;
        GoogleCalendarClientId      = Preferences.Default.Get(StringConsts.GoogleCalendarClientIdPrefKey, string.Empty);
        GoogleCalendarStatusText    = googleCalendar.HasToken ? "Connected" : "Not connected";

        _cocoFactory                 = services.GetService<ICocoApiClientFactory>();
        CocoBaseUrl                 = Preferences.Default.Get(StringConsts.CocoBaseUrlPrefKey,                  StringConsts.CocoDefaultBaseUrl);
        CocoEnabled                 = Preferences.Default.Get(StringConsts.CocoEnabledPrefKey,                  false);
        CocoProjectPath             = Preferences.Default.Get(StringConsts.CocoProjectPathPrefKey,              string.Empty);
        CocoClipboardMonitorEnabled = Preferences.Default.Get(StringConsts.CocoClipboardMonitorEnabledPrefKey,  true);
        CocoHotkey                  = Preferences.Default.Get(StringConsts.CocoHotkeyPrefKey,                   StringConsts.CocoDefaultHotkey);
        var isProd                    = BuildEnvironment.Name.EqualsIgnoreCase("PROD");
        var defaultDiagnosticsEnabled = !isProd;

        EnableStartupProbes         = Preferences.Default.Get(StringConsts.EnableStartupProbesPrefKey,          true);
        EnableStartupDiagnostics    = Preferences.Default.Get(StringConsts.EnableStartupDiagnosticsPrefKey,     defaultDiagnosticsEnabled);
        StreamingEnabled            = Preferences.Default.Get(StringConsts.StreamingEnabledPrefKey,              true);
        SelectedTheme               = Preferences.Default.Get("AppThemePreference",                             "System");
    }

    public Task RefreshHealthStatusAsync() => RefreshHealthStatus();

    [RelayCommand]
    private async Task RefreshHealthStatus()
    {
        if (_healthConnect is null) return;
        HealthStatusText = await _healthConnect.CheckPermissionsAsync()
            ? "Connected — permissions granted"
            : "Not connected";
    }

    [RelayCommand]
    private async Task ConnectHealth()
    {
        if (_healthConnect is null) return;
        await _healthConnect.RequestPermissionsAsync();
        await RefreshHealthStatus();
    }

    [RelayCommand]
    private async Task ForcePushHealth()
    {
        if (_healthPushService is null)
        {
            HealthStatusText = "Error: HealthPushService not registered";
            return;
        }

        HealthStatusText = "Pushing snapshot…";
        try
        {
            var result = await _healthPushService.PushNowAsync();
            if (result.Success)
            {
                HealthStatusText = "Push successful: " + result.Message;
            }
            else
            {
                HealthStatusText = "Push failed: " + result.Message;
            }
        }
        catch (Exception ex)
        {
            HealthStatusText = $"Push failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyPersonality()
    {
        // TODO: Add ability to set the personality from the UI
       // _personalityService.SetCurrent("Programmer");
        
        var currentPersonality = _personalityService.Current;
        
        _configService.UpdateConfig(currentPersonality.OllamConfiguration);
        
        //TODO: UI fields not updating (this is not being hit when the page opens)
        Model       = currentPersonality.OllamConfiguration.Model;
        NumPredict  = currentPersonality.OllamConfiguration.NumPredict;
        Temperature = currentPersonality.OllamConfiguration.Temperature;
        Endpoint    = currentPersonality.OllamConfiguration.Host;
    }
    
    [RelayCommand]
    private void Save()
    {
        var newConfig = new OllamaConfig
                        {
                            Model       = Model
                          , NumPredict  = NumPredict
                          , Temperature = Temperature
                          , Host        = Endpoint
                        };

        _configService.UpdateConfig(newConfig);

        Preferences.Default.Set(StringConsts.TtsProviderPrefKey,      SelectedTtsProvider);
        Preferences.Default.Set(StringConsts.TtsAzureKeyPrefKey,      TtsAzureKey);
        Preferences.Default.Set(StringConsts.TtsAzureRegionPrefKey,   TtsAzureRegion);
        Preferences.Default.Set(StringConsts.TtsElevenLabsKeyPrefKey, TtsElevenLabsKey);

        Preferences.Default.Set(StringConsts.GoogleCalendarClientIdPrefKey, GoogleCalendarClientId);

        Preferences.Default.Set(StringConsts.CocoBaseUrlPrefKey,                 CocoBaseUrl);
        Preferences.Default.Set(StringConsts.CocoEnabledPrefKey,                 CocoEnabled);
        Preferences.Default.Set(StringConsts.CocoProjectPathPrefKey,             CocoProjectPath);
        Preferences.Default.Set(StringConsts.CocoClipboardMonitorEnabledPrefKey, CocoClipboardMonitorEnabled);
        Preferences.Default.Set(StringConsts.CocoHotkeyPrefKey,                  CocoHotkey);

        Preferences.Default.Set(StringConsts.EnableStartupProbesPrefKey,          EnableStartupProbes);
        Preferences.Default.Set(StringConsts.EnableStartupDiagnosticsPrefKey,     EnableStartupDiagnostics);
        Preferences.Default.Set(StringConsts.StreamingEnabledPrefKey,              StreamingEnabled);
    }

    // ── Google Calendar commands ──────────────────────────────────────────────

    [RelayCommand]
    private async Task ConnectCalendar()
    {
        // Save the client ID before attempting the OAuth flow.
        Preferences.Default.Set(StringConsts.GoogleCalendarClientIdPrefKey, GoogleCalendarClientId);

        if (GoogleCalendarClientId.HasNoValue())
        {
            GoogleCalendarStatusText = "Enter your Client ID first";
            return;
        }

        GoogleCalendarStatusText = "Connecting…";
        var success = await _googleCalendar.ConnectAsync();
        GoogleCalendarStatusText = success ? "Connected" : "Connection failed — check Client ID";
    }

    [RelayCommand]
    private async Task DisconnectCalendar()
    {
        await _googleCalendar.DisconnectAsync();
        GoogleCalendarStatusText = "Not connected";
    }

    // ── Coco commands ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshCocoStatus()
    {
        if (_cocoFactory is null)
        {
            CocoStatusText = "Coco client not registered";
            return;
        }

        CocoStatusText = "Checking…";
        var coco   = _cocoFactory.Create();
        var status = await coco.GetStatusAsync();
        CocoStatusText = status.Summary;
    }

    private CancellationTokenSource? _indexCts;

    [RelayCommand]
    private async Task IndexCocoPath()
    {
        if (_cocoFactory is null)
        {
            CocoStatusText = "Coco client not registered";
            return;
        }

        if (CocoProjectPath.HasNoValue())
        {
            CocoStatusText = "Enter a project path first";
            return;
        }

        _indexCts?.Cancel();
        _indexCts = new CancellationTokenSource();
        var token  = _indexCts.Token;

        IsIndexing     = true;
        CocoStatusText = "Starting index…";

        try
        {
            var coco = _cocoFactory.Create();

            await foreach (var ev in coco.IndexStreamAsync(CocoProjectPath, force: false, token))
            {
                if (token.IsCancellationRequested) break;

                CocoStatusText = ev.Total.HasValue
                    ? $"Indexing {ev.Processed}/{ev.Total} — {ev.CurrentFile ?? ev.Message}"
                    : ev.Message ?? ev.Status ?? "Indexing…";

                if (ev.IsCompleted)
                {
                    CocoStatusText = $"Index complete — refreshing status…";
                    break;
                }

                if (ev.IsError)
                {
                    CocoStatusText = $"Index error: {ev.Message}";
                    return;
                }
            }

            if (!token.IsCancellationRequested)
                await RefreshCocoStatus();
        }
        catch (OperationCanceledException)
        {
            CocoStatusText = "Indexing cancelled";
        }
        finally
        {
            IsIndexing = false;
        }
    }

    [RelayCommand]
    private void CancelIndex()
    {
        _indexCts?.Cancel();
    }

    [RelayCommand]
    public async Task SendTestNotificationAsync()
    {
        _loggingService?.LogInformation("SendTestNotificationAsync command triggered in settings view model.", Category.App);

        if (Microsoft.Maui.Devices.DeviceInfo.Current.Platform == Microsoft.Maui.Devices.DevicePlatform.WinUI)
        {
            throw new PlatformNotSupportedException(
                "Windows unpackaged development builds do not support local OS notifications (due to UWP/MSIX activation constraints).\n\n" +
                "To verify and test notifications, please run the app on the Android emulator or a physical device.");
        }

        if (_notificationScheduler is null)
        {
            _loggingService?.LogWarning("NotificationScheduler is null in SettingsViewModel. Cannot send notification.", Category.App);
            throw new InvalidOperationException("NotificationScheduler is null in DI container.");
        }

        if (!await _notificationScheduler.AreNotificationsEnabledAsync())
        {
            _loggingService?.LogInformation("Notifications not enabled. Requesting permission...", Category.App);
            var permissionGranted = await _notificationScheduler.RequestPermissionAsync();
            if (!permissionGranted)
            {
                _loggingService?.LogWarning("Notification permission denied.", Category.App);
                throw new UnauthorizedAccessException(
                    "Notification permission was denied. Please enable notifications for this app in your Android system settings to test notifications.");
            }
            _loggingService?.LogInformation("Notification permission granted.", Category.App);
        }

        try
        {
            _loggingService?.LogInformation("Scheduling test notification to fire in 5 seconds...", Category.App);
            await _notificationScheduler.ScheduleAsync(
                9999
              , "Test Notification ⚡"
              , "This is a test notification from the Cognitive Platform client."
              , DateTime.Now.AddSeconds(5)
              , "cp-reminders");
            _loggingService?.LogInformation("Test notification successfully scheduled.", Category.App);
        }
        catch (Exception ex)
        {
            _loggingService?.LogError(ex, "Failed to schedule test notification.", Category.App);
            throw;
        }
    }
}
