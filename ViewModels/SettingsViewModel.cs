
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly OllamaConfigService      _configService;
    private readonly IPersonalityService      _personalityService;
    private readonly ApiEnvironmentDescriptor _apiEnvironment;
    private readonly AppShellMasterViewModel  _appShellMasterViewModel;
    
    [ObservableProperty] private string _model;
    [ObservableProperty] private string _endpoint;
    [ObservableProperty] private int    _numPredict;
    [ObservableProperty] private float  _temperature;
    [ObservableProperty] private string _environment;
    
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
                            , AppShellMasterViewModel  appShellMasterViewModel)
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
    }
}
