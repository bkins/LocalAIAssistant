using CP.Client.Core.Avails;
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


    public MainPage( MainViewModel   mainViewModel
                   , ILoggingService logger
                   , ChatViewModel   chatViewModel )
    {
        InitializeComponent();

        //var llmService = new LlmService(new PersonalityService());
        _mainViewModel = mainViewModel;
        ChatViewModel  = chatViewModel;

        BindingContext = ChatViewModel;

        _logger = logger;
        //                       Application startup. Verifying log file write.
        _logger.LogWarning($"{_mainViewModel.ApiEnvironmentDescriptor.Name}{Environment.NewLine}{_mainViewModel.ApiEnvironmentDescriptor.BaseUrl}"
                         , Category.MainPage);

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

        await Root.FadeTo(1, 250, Easing.Linear);
        
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

        // Fire and forget pulsing background
        // _ = RunPulseLoopAsync(_pulseCts.Token);
        _ = RunHeartPulseLoopAsync(_pulseCts.Token, 15D);
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
        // Fade range:
        const double minOpacity = 0.10;
        const double maxOpacity = 0.25;

        // number of ms up + number of ms down = number of ms x2 cycle
        const uint halfCycleMs = 7500; //15000;

        // Ensure known starting state
        BackgroundGlyph.Opacity = minOpacity;

        while (ct.IsCancellationRequested.Not())
        {
            await BackgroundGlyph.FadeTo(maxOpacity
                                       , halfCycleMs
                                       , Easing.SinInOut);
            if (ct.IsCancellationRequested) break;

            await BackgroundGlyph.FadeTo(minOpacity
                                       , halfCycleMs
                                       , Easing.SinInOut);
        }
    }

    private async Task RunHeartPulseLoopAsync(CancellationToken ct, double beatsPerMinute = 60)
    {
        const double restingOpacity = 0.10;//0.10;
        const double firstBeatPeak  = 0.15;//0.30;
        const double secondBeatPeak = 0.13;//0.22;

        // Derive all timing from BPM
        var cycleDurationMs = (uint)(60_000          / beatsPerMinute);
        var beatRiseMs      = (uint)(cycleDurationMs * 0.08);
        var beatFallMs      = (uint)(cycleDurationMs * 0.13);
        var betweenBeatsMs  = (uint)(cycleDurationMs * 0.08);
        var restMs          = (uint)(cycleDurationMs * 0.55); // remainder is the long rest

        BackgroundGlyph.Opacity = restingOpacity;

        while (ct.IsCancellationRequested.Not())
        {
            // "lub"
            await BackgroundGlyph.FadeTo(firstBeatPeak, beatRiseMs, Easing.CubicIn);
            if (ct.IsCancellationRequested) break;
            await BackgroundGlyph.FadeTo(restingOpacity, beatFallMs, Easing.CubicOut);
            if (ct.IsCancellationRequested) break;

            await Task.Delay((int)betweenBeatsMs, ct);
            if (ct.IsCancellationRequested) break;

            // "dub"
            await BackgroundGlyph.FadeTo(secondBeatPeak, beatRiseMs, Easing.CubicIn);
            if (ct.IsCancellationRequested) break;
            await BackgroundGlyph.FadeTo(restingOpacity, beatFallMs, Easing.CubicOut);
            if (ct.IsCancellationRequested) break;

            await Task.Delay((int)restMs, ct);
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