namespace LocalAIAssistant.Services.Interfaces;

public interface IClipboardMonitorService
{
    event EventHandler<string>? CodeDetected;
    bool IsEnabled { get; set; }
    void Start();
    void Stop();
}
