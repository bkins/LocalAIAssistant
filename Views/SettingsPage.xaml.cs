using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = ((SettingsViewModel)BindingContext).RefreshHealthStatusAsync();
    }
}