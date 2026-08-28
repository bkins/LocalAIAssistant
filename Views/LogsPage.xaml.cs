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
}