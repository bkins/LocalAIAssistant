namespace LocalAIAssistant.Knowledge.Inbox;

public partial class KnowledgeInboxPage : ContentPage
{
    private KnowledgeInboxViewModel ViewModel
        => (KnowledgeInboxViewModel)BindingContext;

    public KnowledgeInboxPage(KnowledgeInboxViewModel viewModel)
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