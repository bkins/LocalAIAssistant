using LocalAIAssistant.Services.Logging;
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

    private async void OnFilterToggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is LogsViewModel vm)
        {
            // Execute the ApplyFilters command to re-load the logs
            await vm.ApplyFiltersCommand.ExecuteAsync(null);
        }
    }
}
// using LocalAIAssistant.Services.Logging;
// using LocalAIAssistant.ViewModels;
//
// namespace LocalAIAssistant.Views;
//
// public partial class LogsPage : ContentPage
// {
//     public LogsPage(LogsViewModel viewModel)
//     {
//         InitializeComponent();
//         BindingContext = viewModel;
//     }
//     
//     protected override async void OnAppearing()
//     {
//         base.OnAppearing();
//
//         if (BindingContext is LogsViewModel vm)
//         {
//             await vm.LoadLogsCommand.ExecuteAsync(null);
//         }
//     }
//     private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
//     {
//         if (e.CurrentSelection.FirstOrDefault() is LogEntry selectedEntry)
//         {
//             await Shell.Current.GoToAsync(nameof(LogDetailPage), 
//                                           new Dictionary<string, object>
//                                           {
//                                               { "LogEntry", selectedEntry }
//                                           });
//
//             // optional: clear selection so the same item can be tapped again later
//             ((CollectionView)sender).SelectedItem = null;
//         }
//     }
//
// } 