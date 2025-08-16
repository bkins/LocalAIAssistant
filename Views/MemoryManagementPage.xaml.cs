using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class MemoryManagementPage : ContentPage
{
    private readonly MemoryManagementViewModel _viewModel;
    
    public MemoryManagementPage(MemoryManagementViewModel viewModel)
    {
        InitializeComponent();  
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        System.Diagnostics.Debug.WriteLine($"MemoryManagementPage BindingContext is {BindingContext?.GetType().Name}");
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Safely call LoadAsync to refresh the data
        if (BindingContext is MemoryManagementViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
    // protected override async void OnAppearing()
    // {
    //     base.OnAppearing();
    //     if (BindingContext is MemoryManagementViewModel vm)
    //     {
    //         await vm.LoadAsync();
    //     }
    // }
   

    private async void ClearShorTermButton_OnClicked(object? sender
                                             , EventArgs e)
    {
        await _viewModel.ClearShortTermAsync();
    }

    private async void ClearLongTermButton_OnClicked(object? sender
                                             , EventArgs e)
    {
        await _viewModel.ClearLongTermAsync();
    }

    private async void RefreshButton_OnClicked(object? sender
                                       , EventArgs e)
    {
        await _viewModel.RefreshAsync();
    }

}