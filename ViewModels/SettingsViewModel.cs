
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
    
    [ObservableProperty] private string _model;
    [ObservableProperty] private string _endpoint;
    [ObservableProperty] private int    _numPredict;
    [ObservableProperty] private float  _temperature;
    [ObservableProperty] private string _environment;

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

        _selectedEnvironment = _apiEnvironment.Name;
        _configService       = configService;
        _personalityService  = personalityService;

        // Load the current config
        var cfg = _configService.GetConfig();

        _model       = cfg.Model;
        _numPredict  = cfg.NumPredict;
        _temperature = cfg.Temperature;
        _endpoint    = cfg.Host;

        _appShellMasterViewModel = appShellMasterViewModel;

        _healthConnect = services.GetService<IHealthConnectManager>();
        if (_healthConnect is not null)
            _ = RefreshHealthStatus();

        _selectedTtsProvider  = Preferences.Default.Get(StringConsts.TtsProviderPrefKey,      TtsProvider.Maui);
        _ttsAzureKey          = Preferences.Default.Get(StringConsts.TtsAzureKeyPrefKey,      string.Empty);
        _ttsAzureRegion       = Preferences.Default.Get(StringConsts.TtsAzureRegionPrefKey,   "eastus");
        _ttsElevenLabsKey     = Preferences.Default.Get(StringConsts.TtsElevenLabsKeyPrefKey, string.Empty);

        _googleCalendar              = googleCalendar;
        _googleCalendarClientId      = Preferences.Default.Get(StringConsts.GoogleCalendarClientIdPrefKey, string.Empty);
        _googleCalendarStatusText    = googleCalendar.HasToken ? "Connected" : "Not connected";

        _cocoFactory                 = services.GetService<ICocoApiClientFactory>();
        _cocoBaseUrl                 = Preferences.Default.Get(StringConsts.CocoBaseUrlPrefKey,                  StringConsts.CocoDefaultBaseUrl);
        _cocoEnabled                 = Preferences.Default.Get(StringConsts.CocoEnabledPrefKey,                  false);
        _cocoProjectPath             = Preferences.Default.Get(StringConsts.CocoProjectPathPrefKey,              string.Empty);
        _cocoClipboardMonitorEnabled = Preferences.Default.Get(StringConsts.CocoClipboardMonitorEnabledPrefKey,  true);
        _cocoHotkey                  = Preferences.Default.Get(StringConsts.CocoHotkeyPrefKey,                   StringConsts.CocoDefaultHotkey);
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
    private void ApplyPersonality()
    {
        // TODO: Add ability to set the personality from the UI
       // _personalityService.SetCurrent("Programmer");
        
        var currentPersonality = _personalityService.Current;
        
        _configService.UpdateConfig(currentPersonality.OllamConfiguration);
        
        //TODO: UI fields not updating (this is not being hit when the page opens)
        _model       = currentPersonality.OllamConfiguration.Model;
        _numPredict  = currentPersonality.OllamConfiguration.NumPredict;
        _temperature = currentPersonality.OllamConfiguration.Temperature;
        _endpoint    = currentPersonality.OllamConfiguration.Host;
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
    }

    // ── Google Calendar commands ──────────────────────────────────────────────

    [RelayCommand]
    private async Task ConnectCalendar()
    {
        // Save the client ID before attempting the OAuth flow.
        Preferences.Default.Set(StringConsts.GoogleCalendarClientIdPrefKey, GoogleCalendarClientId);

        if (string.IsNullOrWhiteSpace(GoogleCalendarClientId))
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

        if (string.IsNullOrWhiteSpace(CocoProjectPath))
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
}
