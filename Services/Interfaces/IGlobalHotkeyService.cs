namespace LocalAIAssistant.Services.Interfaces;

public interface IGlobalHotkeyService
{
    event EventHandler? HotkeyPressed;
    void Register(string hotkeyString);
    void Unregister();
}
