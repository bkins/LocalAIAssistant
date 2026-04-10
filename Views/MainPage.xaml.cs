using System.Collections.Specialized;
using CP.Client.Core.Avails;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class MainPage : ContentPage
{
    private readonly ILoggingService _logger;
    private readonly MainViewModel   _mainViewModel;

    private bool                     _isPageActive;
    private bool                     _isPulsing;
    private CancellationTokenSource? _pulseCts;

    public ChatViewModel ChatViewModel { get; }

    public MainPage( MainViewModel   mainViewModel
                   , ILoggingService logger
                   , ChatViewModel   chatViewModel )
    {
        InitializeComponent();

        _mainViewModel = mainViewModel;
        ChatViewModel  = chatViewModel;

        BindingContext = ChatViewModel;
        // ChatViewModel.Messages.CollectionChanged += (s, e) => 
        // {
        //     if (ChatViewModel.Messages.Count > 0)
        //     {
        //         MainThread.BeginInvokeOnMainThread(() =>
        //         {
        //             Task.Delay(50); // Allow the new message to render before scrolling.
        //             MessagesView.ScrollTo(ChatViewModel.Messages.Count - 1
        //                                 , position: ScrollToPosition.End
        //                                 , animate: true);
        //         });
        //     }
        // };
        
        _logger = logger;
        _logger.LogWarning($"{_mainViewModel.ApiEnvironmentDescriptor.Name}{Environment.NewLine}{_mainViewModel.ApiEnvironmentDescriptor.BaseUrl}"
                         , Category.MainPage);

#if DEBUG && false
        var harness = new TestHarness(_logger);
        harness.RunAll();
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _isPageActive = true;
        StartBackgroundPulse();

        await Root.FadeTo(1, 250, Easing.Linear);

        await ChatViewModel.InitializeAsync();

        // The view owns scroll behaviour — wire up here, tear down in
        // OnDisappearing to avoid double-subscription on re-navigation.
        ChatViewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        ChatViewModel.PropertyChanged            += OnChatViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        _isPageActive = false;
        ChatViewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        ChatViewModel.PropertyChanged            -= OnChatViewModelPropertyChanged;
        StopBackgroundPulse();
        base.OnDisappearing();
    }

    // ── Scroll management ─────────────────────────────────────────────────────

    private async void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only scroll when a new message arrives, not on Clear() or Remove().
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        
        // Yield allows the UI thread to process the layout of the newly added message
        await Task.Yield();
        var lastMessage = ChatViewModel.Messages.LastOrDefault();
        if (lastMessage is null) return;

        Dispatcher.Dispatch(() =>
            MessagesView.ScrollTo(lastMessage
                                , position: ScrollToPosition.End
                                , animate: true));
    }

    private async void OnChatViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Scroll to the bottom when a response finishes (IsTyping: true → false).
        // Content changes happen in-place on the assistant message so CollectionChanged
        // never fires for them; this is the reliable "turn complete" signal.
        if (e.PropertyName != nameof(ChatViewModel.IsTyping)) return;
        if (ChatViewModel.IsTyping) return;

        // Give the layout pass time to measure the final content before scrolling.
        await Task.Delay(100);
        var lastMessage = ChatViewModel.Messages.LastOrDefault();
        if (lastMessage is null) return;
        MessagesView.ScrollTo(lastMessage, position: ScrollToPosition.End, animate: true);
    }

    // ── Input handlers ────────────────────────────────────────────────────────

    // Both the Editor's Completed event and the Entry's Completed event
    // funnel through here — one path, one command.
    // UX-01: guard _isPageActive so that keyboard-dismiss events fired during
    // navigation (app going to background, shell page change) don't submit
    // whatever text is sitting in the editor.
    private void OnEntryCompleted(object? sender, EventArgs e)
    {
        if (!_isPageActive) return;
        if (ChatViewModel.SendCommand.CanExecute(null))
            ChatViewModel.SendCommand.Execute(null);
    }

    public static Keyboard CreateKeyboard =>
        Keyboard.Create(KeyboardFlags.CapitalizeSentence | KeyboardFlags.Suggestions);

    // ── Background pulse animation ────────────────────────────────────────────

    private void StartBackgroundPulse()
    {
        if (_isPulsing)
            return;

        if (BackgroundGlyph == null)
            return;

        _isPulsing = true;
        _pulseCts  = new CancellationTokenSource();

        _ = RunHeartPulseLoopAsync(_pulseCts.Token, 15D);
    }

    private void StopBackgroundPulse()
    {
        _isPulsing = false;

        try
        {
            _pulseCts?.Cancel();
        }
        catch
        {
            // Gulp
        }
        finally
        {
            _pulseCts?.Dispose();
            _pulseCts = null;
        }
    }

    private async Task RunHeartPulseLoopAsync( CancellationToken ct
                                             , double            beatsPerMinute = 60 )
    {
        const double restingOpacity = 0.10;
        const double firstBeatPeak  = 0.15;
        const double secondBeatPeak = 0.13;

        var cycleDurationMs = (uint)(60_000          / beatsPerMinute);
        var beatRiseMs      = (uint)(cycleDurationMs * 0.08);
        var beatFallMs      = (uint)(cycleDurationMs * 0.13);
        var betweenBeatsMs  = (uint)(cycleDurationMs * 0.08);
        var restMs          = (uint)(cycleDurationMs * 0.55);

        BackgroundGlyph.Opacity = restingOpacity;

        while (ct.IsCancellationRequested.Not())
        {
            // "lub"
            await BackgroundGlyph.FadeTo(firstBeatPeak,  beatRiseMs, Easing.CubicIn);
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

    private async Task RunPulseLoopAsync(CancellationToken ct)
    {
        const double minOpacity  = 0.10;
        const double maxOpacity  = 0.25;
        const uint   halfCycleMs = 7500;

        BackgroundGlyph.Opacity = minOpacity;

        while (ct.IsCancellationRequested.Not())
        {
            await BackgroundGlyph.FadeTo(maxOpacity, halfCycleMs, Easing.SinInOut);
            if (ct.IsCancellationRequested) break;

            await BackgroundGlyph.FadeTo(minOpacity, halfCycleMs, Easing.SinInOut);
        }
    }

    // private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    // {
    //     // If the user is more than 200 pixels from the bottom, show the button
    //     // e.VerticalOffset is the current position, e.VerticalDelta is change.
    //     // Note: Finding the 'bottom' exactly is hard in MAUI, so we check if they are 
    //     // scrolling up or if they are significantly far from the 'end'.
    //
    //     // Simple logic: Show if they have scrolled away from the very bottom
    //     // Adjust '200' to your preference for sensitivity.
    //     bool isAwayFromBottom = e.VerticalOffset < (MessagesView.Height - 200); 
    //
    //     // To make it feel better, we only show it if they are actually moving.
    //     ScrollToBottomButton.IsVisible = e.VerticalOffset > 100 && isAwayFromBottom;
    // }
    //
    // private void OnScrollToBottomClicked(object? sender, EventArgs e)
    // {
    //     if (ChatViewModel.Messages.Count > 0)
    //     {
    //         MessagesView.ScrollTo(ChatViewModel.Messages.Count - 1, position: ScrollToPosition.End, animate: true);
    //     }
    //}
}
