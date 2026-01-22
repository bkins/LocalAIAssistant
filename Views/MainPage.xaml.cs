using System.Collections.Specialized;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class MainPage : ContentPage
{
    
    private readonly ILoggingService _logger;
    private readonly MainViewModel _mainViewModel;

    private bool                     _isPulsing;
    private CancellationTokenSource? _pulseCts;
    
    public ChatViewModel ChatViewModel { get; }


    public MainPage(MainViewModel    mainViewModel
                   , ILoggingService logger
                  , ChatViewModel    chatViewModel)
    {
        InitializeComponent();

        //var llmService = new LlmService(new PersonalityService());
        _mainViewModel = mainViewModel;
        ChatViewModel  = chatViewModel;
        
        BindingContext = ChatViewModel;
        
        _logger = logger;
        
#if DEBUG && false
        //logger.LogError(new Exception("TEST"), "Error" );
        
        var harness = new TestHarness(_logger);
        harness.RunAll();
#endif

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        StartBackgroundPulse();
        
        // if (Math.Abs(Root.Opacity - 1) < 0.1)
        //     return;

        await Root.FadeTo(1, 150, Easing.Linear);
        
        await ChatViewModel.InitializeAsync();

        // Subscribe to the CollectionChanged event of the ObservableCollection
        ChatViewModel.ScrollToBottomRequested += () =>
        {
            // Use the Dispatcher to ensure the UI update runs on the main thread
            Dispatcher.Dispatch(() =>
            {
                // Check if there are messages before attempting to scroll
                if (ChatViewModel.Messages.Count <= 0) return;
                
                var lastItem = ChatViewModel.Messages.LastOrDefault();
                MessagesView.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
            });
        };

    }
    
    protected override void OnDisappearing()
    {
        StopBackgroundPulse();
        base.OnDisappearing();
    }

    private void StartBackgroundPulse()
    {
        if (_isPulsing)
            return;

        if (BackgroundGlyph == null)
            return;

        _isPulsing = true;
        _pulseCts  = new CancellationTokenSource();

        // Fire and forget safely
        _ = RunPulseLoopAsync(_pulseCts.Token);
    }

    private void StopBackgroundPulse()
    {
        _isPulsing = false;

        try
        {
            _pulseCts?.Cancel();
        }
        catch { /* ignore */ }
        finally
        {
            _pulseCts?.Dispose();
            _pulseCts = null;
        }
    }

    private async Task RunPulseLoopAsync(CancellationToken ct)
    {
        // Choose your range:
        const double minOpacity = 0.06;
        const double maxOpacity = 0.09;

        // 15s up + 15s down = 30s cycle
        const uint halfCycleMs = 15000;

        // Ensure known starting state
        BackgroundGlyph.Opacity = minOpacity;

        while (!ct.IsCancellationRequested)
        {
            await BackgroundGlyph.FadeTo(maxOpacity, halfCycleMs, Easing.SinInOut);
            if (ct.IsCancellationRequested) break;

            await BackgroundGlyph.FadeTo(minOpacity, halfCycleMs, Easing.SinInOut);
        }
    }

    private async void PromptEditor_Completed(object sender, EventArgs e)
    {
        await _mainViewModel.SendPromptAsync();
    }

    
    public static Keyboard CreateKeyboard =>
        Keyboard.Create(KeyboardFlags.CapitalizeSentence | KeyboardFlags.Suggestions);

    private void OnEntryCompleted (object?   sender
                                 , EventArgs e)
    {
        if (BindingContext is ChatViewModel vm 
          && vm.SendCommand.CanExecute(null))
        {
            vm.SendCommand.Execute(null);
        }
    }
}