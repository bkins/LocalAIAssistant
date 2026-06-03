using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Services;

public sealed class NullGlobalHotkeyService : IGlobalHotkeyService
{
    public event EventHandler? HotkeyPressed;
    public void Register(string hotkeyString) { }
    public void Unregister()                  { }
}
