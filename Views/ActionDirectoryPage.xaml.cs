using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class ActionDirectoryPage : ContentPage
{
    public ActionDirectoryPage(ActionDirectoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
