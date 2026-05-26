using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class ConversationsPage : ContentPage
{
    private readonly ConversationsViewModel _viewModel;

    public ConversationsPage(ConversationsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel   = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.LoadAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DisplayAlert("Error", "Could not load conversations.", "OK");
        }
    }
}
