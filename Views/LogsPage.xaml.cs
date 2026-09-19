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
