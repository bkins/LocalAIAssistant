using LocalAIAssistant.Knowledge.Tasks.ViewModels;

namespace LocalAIAssistant.Knowledge.Tasks.Views;

public partial class EditTaskPage : ContentPage
{
    private readonly EditTaskViewModel _viewModel;

    public EditTaskPage(EditTaskViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
