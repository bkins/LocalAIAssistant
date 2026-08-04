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

    private async void OnSendTestNotificationClicked(object sender, EventArgs e)
    {
        if (BindingContext is SettingsViewModel vm)
        {
            try
            {
                await vm.SendTestNotificationAsync();
                await DisplayAlert("Test Notification Success", "Test notification was successfully scheduled to fire in 5 seconds!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Test Notification Error", $"Failed to schedule notification.\n\nError: {ex.Message}\nType: {ex.GetType().Name}", "OK");
            }
        }
    }
}