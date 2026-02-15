using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class SettingsPage : ContentPage
{

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}