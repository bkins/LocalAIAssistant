using System.Collections.Specialized;
using CP.Client.Core.Avails;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class MainPage : ContentPage
{
    private readonly ILoggingService _logger;
    private readonly MainViewModel   _mainViewModel;
    private readonly ChatScrollState _chatScrollState = new();

    private bool                     _isPageActive;
    private bool                     _isPinningLatest;
    private bool                     _isPulsing;
    private CancellationTokenSource? _bottomPinCts;
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

#if WINDOWS
        ChatEditor.HandlerChanged += OnChatEditorHandlerChanged;
        ChatEditor.Loaded         += OnChatEditorLoaded;
#endif

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

        if (!ChatViewModel.HasBeenInitialized)
            await ChatViewModel.InitializeAsync();
        else
            ChatViewModel.RefreshCocoState();

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
        CancelBottomPin();
        StopBackgroundPulse();
        _ = ChatViewModel.StopSpeakingAsync();
        base.OnDisappearing();
    }

    // ── Scroll management ─────────────────────────────────────────────────────

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only scroll when a new message arrives, not on Clear() or Remove().
        if (e.Action != NotifyCollectionChangedAction.Add) return;

        if (e.NewItems?.OfType<Message>().Any(message => message.Sender == "user") == true)
            _chatScrollState.MarkPromptSent();

        ScheduleBottomPin();
    }

    private void OnChatViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Scroll to the bottom when a response finishes (IsTyping: true → false).
        // Content changes happen in-place on the assistant message so CollectionChanged
        // never fires for them; this is the reliable "turn complete" signal.
        if (e.PropertyName != nameof(ChatViewModel.IsTyping)) return;
        if (ChatViewModel.IsTyping) return;

        ScheduleBottomPin();
    }

    private void OnMessagesViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (_isPinningLatest) return;

        _chatScrollState.ObserveViewport(e.LastVisibleItemIndex, ChatViewModel.Messages.Count);
        if (_chatScrollState.IsFollowingLatest.Not())
            CancelBottomPin();
    }

    private void OnMessageContentSizeChanged(object? sender, EventArgs e)
    {
        ScheduleBottomPin();
    }

    private void ScheduleBottomPin()
    {
        if (_isPageActive.Not() || _chatScrollState.ShouldPinAfterContentChange.Not()) return;

        CancelBottomPin();
        _bottomPinCts = new CancellationTokenSource();
        _ = SettleAtBottomAsync(_bottomPinCts.Token);
    }

    private async Task SettleAtBottomAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var delayMilliseconds in new[] { 0, 50, 150, 300 })
            {
                if (delayMilliseconds > 0)
                    await Task.Delay(delayMilliseconds, cancellationToken);

                if (_isPageActive.Not() || _chatScrollState.ShouldPinAfterContentChange.Not()) return;
                ScrollToBottom();
            }
        }
        catch (OperationCanceledException)
        {
            // A newer layout pass or intentional upward scroll superseded this request.
        }
    }

    private void CancelBottomPin()
    {
        try
        {
            _bottomPinCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Cancellation raced with page teardown.
        }
        finally
        {
            _bottomPinCts?.Dispose();
            _bottomPinCts = null;
        }
    }

    private void ScrollToBottom()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var lastMessage = ChatViewModel.Messages.LastOrDefault();
            if (lastMessage is null) return;

            try
            {
                _isPinningLatest = true;
                MessagesView.ScrollTo(lastMessage
                                    , position: ScrollToPosition.End
                                    , animate: false);
            }
            catch
            {
                // Guard against potential transient scroll errors during view teardown
            }
            finally
            {
                _isPinningLatest = false;
            }
        });
    }

    // ── Input handlers ────────────────────────────────────────────────────────

#if WINDOWS
    private void OnChatEditorLoaded(object? sender, EventArgs e)
    {
        AttachWindowsKeyHandler();
    }

    private void OnChatEditorHandlerChanged(object? sender, EventArgs e)
    {
        AttachWindowsKeyHandler();
    }

    private void AttachWindowsKeyHandler()
    {
        if (ChatEditor.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            textBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
            textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
        }
    }

    private void OnTextBoxPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (!_isPageActive) return;

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (ChatViewModel.IsSuggestionsVisible)
            {
                ChatViewModel.DismissSuggestions();
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Windows.System.VirtualKey.Up)
        {
            if (sender is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                var text = tb.Text ?? string.Empty;
                var caret = tb.SelectionStart;
                var isSingleLine = !text.Contains('\n');
                var isAtTop = caret == 0 || (caret <= text.Length && !text[..caret].Contains('\n'));

                if (isSingleLine || isAtTop)
                {
                    if (ChatViewModel.TryRecallPreviousPrompt(out var recalled))
                    {
                        ChatViewModel.PromptText = recalled;
                        tb.Text = recalled;
                        tb.SelectionStart = recalled.Length;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        if (e.Key == Windows.System.VirtualKey.Down)
        {
            if (sender is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                var text = tb.Text ?? string.Empty;
                var caret = tb.SelectionStart;
                var isSingleLine = !text.Contains('\n');
                var isAtBottom = caret == text.Length || (caret <= text.Length && !text[caret..].Contains('\n'));

                if (isSingleLine || isAtBottom)
                {
                    if (ChatViewModel.TryRecallNextPrompt(out var recalled))
                    {
                        ChatViewModel.PromptText = recalled;
                        tb.Text = recalled;
                        tb.SelectionStart = recalled.Length;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var isShiftDown = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                                                                     .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var isCtrlDown  = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                                                                     .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (!isShiftDown && !isCtrlDown)
            {
                e.Handled = true;

                if (ChatViewModel.SendCommand.CanExecute(null))
                {
                    ChatViewModel.SendCommand.Execute(null);
                }
            }
            // If Shift or Control is pressed, allow WinUI TextBox to insert newline naturally
        }
    }
#endif

    // UX-01: _isPageActive guard — keyboard-dismiss events fired during navigation must not submit.
    // On Windows, Enter-to-send and Shift+Enter multiline insertions are handled via PreviewKeyDown.
    // On Mobile, Editor allows multiline entry naturally; Send button submits prompt.
    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
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
        catch (ObjectDisposedException){ /* Gulp */ }
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

        try
        {
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
        catch (OperationCanceledException)
        {
            // Normal loop exit on page disappearance or pulse cancellation
        }
    }

    private async Task RunPulseLoopAsync(CancellationToken ct)
    {
        const double minOpacity  = 0.10;
        const double maxOpacity  = 0.25;
        const uint   halfCycleMs = 7500;

        BackgroundGlyph.Opacity = minOpacity;

        try
        {
            while (ct.IsCancellationRequested.Not())
            {
                await BackgroundGlyph.FadeTo(maxOpacity, halfCycleMs, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;

                await BackgroundGlyph.FadeTo(minOpacity, halfCycleMs, Easing.SinInOut);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal loop exit on cancellation
        }
    }
    
    private async void OnClearButtonClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert( "Clear messages"
                                          , "Clear all messages? This cannot be undone."
                                          , "Clear"
                                          , "Cancel" );
        if (!confirmed) return;

        await ChatViewModel.ClearMessagesAsync();
    }

    private async void OnCopyMessageClicked(object sender, EventArgs e)
    {
        if (sender is not Button copyButton)
            return;

        if (copyButton.BindingContext is not Message { Content.Length: > 0 } message)
            return;

        await Clipboard.Default.SetTextAsync(message.Content);

        // Optional: UX feedback
        await DisplayToast("Copied to clipboard");
    }

    private async void OnCalendarConnectClicked(object sender, EventArgs e)
        => await ChatViewModel.ConnectGoogleCalendarAsync();
    
    private async Task DisplayToast(string message)
    {
        await DisplayAlert("", message, "OK");
    }
    
}
