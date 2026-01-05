using LocalAIAssistant.Knowledge.Journals.ViewModels;

namespace LocalAIAssistant.Knowledge.Journals.Views;

public partial class JournalDetailPage : ContentPage
{
    private JournalDetailViewModel ViewModel
        => (JournalDetailViewModel)BindingContext;

    public JournalDetailPage(JournalDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }
}
