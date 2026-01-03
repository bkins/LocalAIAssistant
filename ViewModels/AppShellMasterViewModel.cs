using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalAIAssistant.ViewModels;

public partial class AppShellMasterViewModel : ObservableObject
{
    // These properties will hold your individual view models
    public ApiHealthViewModel ApiHealthViewModel { get; }
    public AppShellViewModel  AppShellViewModel  { get; }

    public AppShellMasterViewModel(ApiHealthViewModel apiHealthViewModel, AppShellViewModel appShellViewModel)
    {
        ApiHealthViewModel = apiHealthViewModel;
        AppShellViewModel  = appShellViewModel;
    }
}