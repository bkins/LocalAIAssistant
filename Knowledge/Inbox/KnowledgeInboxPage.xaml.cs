namespace LocalAIAssistant.Knowledge.Inbox;

public partial class KnowledgeInboxPage : ContentPage
{
    
    //TODO: Filter chips need to be dynamic.
    // if there are zero or one Inbox Kind, then there should be no filters chips, 
    // If there are more than one Inbox Kind, then show filter chips for each type in the Inbox items.
    // Chips should be created based on the Kinds in all Inbox items.
    // `KnowledgeKind` should be removed once this is implemented
    
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