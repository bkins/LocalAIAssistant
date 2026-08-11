using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class AgentChatPage : ContentPage
{
    private readonly AgentChatViewModel _viewModel;

    public AgentChatPage(AgentChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch
        {
            // Ignore / swallow initialization issues
        }
    }
}
