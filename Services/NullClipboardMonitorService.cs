using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Services;

public sealed class NullClipboardMonitorService : IClipboardMonitorService
{
    public event EventHandler<string>? CodeDetected;
    public bool IsEnabled { get; set; }
    public void Start()  { }
    public void Stop()   { }
}
