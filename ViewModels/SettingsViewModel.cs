
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    }
}
