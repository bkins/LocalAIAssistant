using System.Runtime.InteropServices;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Platforms.Windows;

/// <summary>
/// Registers a global hotkey via <c>RegisterHotKey</c> on a dedicated message-loop thread.
/// When the hotkey fires, brings the LAA window to the foreground and raises <see cref="HotkeyPressed"/>.
/// Call <see cref="Unregister"/> on app close to release the Win32 resource.
/// </summary>
public sealed class WindowsHotkeyService : IGlobalHotkeyService, IDisposable
{
    public event EventHandler? HotkeyPressed;

    // Win32 constants
    private const uint WmHotkey      = 0x0312;
    private const uint WmQuit        = 0x0012;
    private const uint ModControl    = 0x0002;
    private const uint ModShift      = 0x0004;
    private const uint ModAlt        = 0x0001;
    private const int  HotkeyId      = 0x3A8C;

    private Thread? _messageThread;
    private int     _messageThreadId;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessage(int idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern int GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hWnd;
        public uint   message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint   time;
        public int    ptX;
        public int    ptY;
    }

    public void Register(string hotkeyString)
    {
        Unregister();

        var (mods, vk) = ParseHotkey(hotkeyString);

        _messageThread = new Thread(() => RunMessageLoop(mods, vk))
                         {
                             IsBackground = true
                           , Name         = "CocoHotkeyListener"
                         };
        _messageThread.Start();
    }

    public void Unregister()
    {
        if (_messageThreadId == 0) return;
        PostThreadMessage(_messageThreadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
        _messageThreadId = 0;
    }

    public void Dispose() => Unregister();

    private void RunMessageLoop(uint mods, uint vk)
    {
        _messageThreadId = GetCurrentThreadId();

        RegisterHotKey(IntPtr.Zero, HotkeyId, mods, vk);

        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            if (msg.message == WmHotkey && msg.wParam.ToInt32() == HotkeyId)
            {
                BringToForeground();
                MainThread.BeginInvokeOnMainThread(() => HotkeyPressed?.Invoke(this, EventArgs.Empty));
            }

            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        UnregisterHotKey(IntPtr.Zero, HotkeyId);
    }

    private static void BringToForeground()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUiWindow)
                winUiWindow.Activate();
        });
    }

    // ── Hotkey string parser ──────────────────────────────────────────────────

    internal static (uint Modifiers, uint VirtualKey) ParseHotkey(string hotkey)
    {
        uint mods = 0;
        uint vk   = 0;

        if (string.IsNullOrWhiteSpace(hotkey))
            return (ModControl | ModShift, 'C');

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control": mods |= ModControl; break;
                case "alt":     mods |= ModAlt;     break;
                case "shift":   mods |= ModShift;   break;
                default:
                    if (part.Length == 1)
                        vk = (uint)char.ToUpper(part[0]);
                    break;
            }
        }

        if (mods == 0) mods = ModControl | ModShift;
        if (vk   == 0) vk   = 'C';

        return (mods, vk);
    }
}
