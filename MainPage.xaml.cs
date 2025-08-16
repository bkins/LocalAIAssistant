using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant;

public partial class MainPage : ContentPage
{
    
    private readonly ILoggingService _logger;
    
    private readonly MainViewModel                  _viewModel;
    
    public MainPage(MainViewModel viewModel, ILoggingService logger)
    {
        InitializeComponent();

        //var llmService = new LlmService(new PersonalityService());
        _viewModel     = viewModel;
        BindingContext = _viewModel;

        _logger = logger;

    }

    private async void PromptEditor_Completed(object sender, EventArgs e)
    {
        await _viewModel.SendPromptAsync();
        _logger.LogInformation(_viewModel.LastResponse);
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        await _viewModel.SendPromptAsync();
    }
    // private readonly LlmService _llmService;
    //
    // public ObservableCollection<Message> Messages { get; set; } = new();
    //
    // public MainPage()
    // {
    //     InitializeComponent();
    //     _llmService    = new LlmService(new PersonalityService());
    //     BindingContext = this;
    // }
    //
    // private async void OnSendClicked(object sender, EventArgs e)
    // {
    //     await SendPromptAsync();
    // }
    //
    // private async Task ScrollToLastMessage()
    // {
    //     await Task.Delay(50); // allow layout to settle
    //
    //     if (Messages.Count == 0) return;
    //
    //     var lastMessage = Messages[^1];
    //     MessagesView.ScrollTo(lastMessage, position: ScrollToPosition.End, animate: true);
    //     
    // }
    //
    // private async void PromptEntry_Completed(object sender, EventArgs e)
    // {
    //     await SendPromptAsync(); // same method as the send button
    // }
    //
    // private async Task SendPromptAsync()
    // {
    //     var prompt = PromptEntry.Text?.Trim();
    //     if (string.IsNullOrEmpty(prompt)) return;
    //
    //     Messages.Add(new Message { Sender = "User", Content = prompt });
    //     PromptEntry.Text = string.Empty;
    //     
    //     Messages.Add(new Message { Sender = "AI", Content = "Thinking..." });
    //     
    //     
    //     var index = Messages.Count - 1;
    //     var response = await Task.Run(() => _llmService.SendPromptAsync(prompt)); //await _llmService.SendPromptAsync(prompt);
    //     Messages[index].Content = response ?? "[No response]";
    //     
    //     await ScrollToLastMessage();
    //     
    //     PromptEntry.Focus();
    // }

}