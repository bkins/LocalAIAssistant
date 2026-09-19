using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class LogsPage : ContentPage
{
    public LogsPage(LogsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LogsViewModel vm)
        {
            await vm.LoadLogsCommand.ExecuteAsync(null);
        }
    }

    private void OnDateFilterSwitchHandlerChanged(object? sender, EventArgs eventArgs)
    {
#if WINDOWS
        if (sender is Switch filterSwitch &&
            filterSwitch.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ToggleSwitch toggleSwitch)
        {
            // WinUI's ToggleSwitch has a large native minimum width. Without overriding
            // it, the MAUI WidthRequest does not bring the adjacent label any closer.
            toggleSwitch.MinWidth = 0;
            toggleSwitch.Width = 52;
            toggleSwitch.MaxWidth = 52;
            toggleSwitch.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
        }
#endif
    }

    private async void OnViewDetailsClicked(object? sender, EventArgs eventArgs)
    {
        if (sender is not Button { CommandParameter: Services.Logging.LogEntry entry }) return;

        try
        {
            await Shell.Current.GoToAsync(nameof(LogDetailPage), new Dictionary<string, object>
            {
                { nameof(Services.Logging.LogEntry), entry }
            });
        }
        catch (Exception exception)
        {
            await DisplayAlert("Unable to open log details", exception.Message, "OK");
        }
    }
}
