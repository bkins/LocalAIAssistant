using LocalAIAssistant.Knowledge.Tasks.ViewModels;

namespace LocalAIAssistant.Knowledge.Tasks.Views;

public partial class TaskDetailPage : ContentPage
{
    private readonly TaskDetailViewModel _viewModel;
    
    public TaskDetailPage(TaskDetailViewModel viewModel)
    {
        InitializeComponent();
        
        BindingContext = viewModel;
        _viewModel     = viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}