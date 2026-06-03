using LocalAIAssistant.Core.Coco;
using LocalAIAssistant.Services.Interfaces;
using WinClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using Windows.ApplicationModel.DataTransfer;

namespace LocalAIAssistant.Platforms.Windows;

public sealed class WindowsClipboardMonitorService : IClipboardMonitorService, IDisposable
{
    public event EventHandler<string>? CodeDetected;

    private bool _isEnabled;
    private bool _started;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public void Start()
    {
        if (_started) return;
        WinClipboard.ContentChanged += OnContentChanged;
        _started = true;
    }

    public void Stop()
    {
        if (!_started) return;
        WinClipboard.ContentChanged -= OnContentChanged;
        _started = false;
    }

    public void Dispose() => Stop();

    private async void OnContentChanged(object sender, object args)
    {
        if (!_isEnabled) return;

        try
        {
            if (!MainThread.IsMainThread)
            {
                await MainThread.InvokeOnMainThreadAsync(ProcessClipboardAsync);
                return;
            }

            await ProcessClipboardAsync();
        }
        catch
        {
            // Clipboard access can fail when another process holds the clipboard — non-fatal.
        }
    }

    private async Task ProcessClipboardAsync()
    {
        try
        {
            var content = WinClipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text)) return;

            var text = await content.GetTextAsync();
            if (!ClipboardCodeDetector.IsCode(text)) return;

            CodeDetected?.Invoke(this, text);
        }
        catch
        {
            // Swallow — clipboard unavailability is non-fatal.
        }
    }
}
