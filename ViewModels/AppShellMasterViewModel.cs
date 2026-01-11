using CommunityToolkit.Mvvm.ComponentModel;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.ViewModels;

public partial class AppShellMasterViewModel : ObservableObject
{
    private ApiEnvironmentService _environment;
    
    // These properties will hold your individual view models
    public ApiHealthViewModel ApiHealthViewModel { get; }
    public AppShellViewModel  AppShellViewModel  { get; }

    public string EnvironmentName => _environment.Current.ToString();

    public AppShellMasterViewModel (ApiHealthViewModel    apiHealthViewModel
                                  , AppShellViewModel     appShellViewModel
                                  , ApiEnvironmentService environment)
    {
        ApiHealthViewModel = apiHealthViewModel;
        AppShellViewModel  = appShellViewModel;
        
        _environment    = environment;
        _environment.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(EnvironmentName));
        };
    }
}