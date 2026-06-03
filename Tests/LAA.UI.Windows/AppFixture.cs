using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

// Alias to avoid ambiguity with System.Windows.Forms.Application on Windows TFM.
using FlaUIApp = FlaUI.Core.Application;

namespace LAA.UI.Windows;

/// <summary>
/// Manages a single FlaUI application session shared across all smoke tests.
/// FlaUI drives the Windows UI Automation (UIA3) API directly — no WinAppDriver
/// server is required.
///
/// xUnit creates one AppFixture per test class (IClassFixture), launching the app
/// once and tearing it down after the last test in the class completes.
/// </summary>
public sealed class AppFixture : IDisposable
{
    public FlaUIApp        App        { get; }
    public AutomationBase  Automation { get; }
    public Window          MainWindow { get; }

    public AppFixture()
    {
        Automation = new UIA3Automation();
        App        = FlaUIApp.Launch(ResolveExePath());
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(30));

        // Give MAUI time to render its full page hierarchy before the first
        // test starts querying the accessibility tree.
        WaitForElement("ChatEditor", timeoutSeconds: 30);
    }

    // ── Element helpers ──────────────────────────────────────────────────────

    /// <summary>Finds an element by its MAUI AutomationId. Returns null on timeout.</summary>
    public AutomationElement? FindById(string automationId, int timeoutSeconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var el = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            if (el is not null) return el;
            Thread.Sleep(300);
        }
        return null;
    }

    /// <summary>Like FindById but throws <see cref="TimeoutException"/> instead of returning null.</summary>
    public AutomationElement WaitForElement(string automationId, int timeoutSeconds = 10)
    {
        var el = FindById(automationId, timeoutSeconds);
        if (el is null)
            throw new TimeoutException(
                $"Element '{automationId}' not found within {timeoutSeconds}s. " +
                "The app may not have rendered this page yet, or the AutomationId is wrong.");
        return el;
    }

    /// <summary>Finds an element by UIA Name (matches tab title, label text, button caption).</summary>
    public AutomationElement? FindByName(string name, int timeoutSeconds = 6)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var el = MainWindow.FindFirstDescendant(cf => cf.ByName(name));
            if (el is not null) return el;
            Thread.Sleep(300);
        }
        return null;
    }

    /// <summary>
    /// Navigates to a Shell tab by clicking the UIA element whose Name matches the
    /// given title (e.g. "Chat", "Chats", "Inbox", "Memory", "Logs", "Settings").
    /// </summary>
    public bool NavigateTo(string tabTitle, int delayMs = 800)
    {
        // MAUI Shell tabs on Windows appear as NavigationViewItem or TabViewItem
        // elements whose UIA Name equals the tab Title string.
        var tab = FindByName(tabTitle, timeoutSeconds: 4);
        if (tab is null) return false;

        // Try SelectionItemPattern (tab items), then InvokePattern (buttons/links).
        var selPat    = tab.Patterns.SelectionItem.PatternOrDefault;
        var invokePat = tab.Patterns.Invoke.PatternOrDefault;
        if      (selPat    is not null) selPat.Select();
        else if (invokePat is not null) invokePat.Invoke();
        // If neither pattern is supported the element is still focusable; the
        // caller will verify navigation completed via FindById / FindByName.

        Thread.Sleep(delayMs);
        return true;
    }

    // ── Path resolution ──────────────────────────────────────────────────────

    private static string ResolveExePath()
    {
        var envPath = Environment.GetEnvironmentVariable("LAA_EXE_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            if (File.Exists(envPath)) return envPath;
            throw new FileNotFoundException(
                $"LAA_EXE_PATH points to a non-existent file: {envPath}");
        }

        var repoRoot = FindRepoRoot();
        var tfm      = "net9.0-windows10.0.19041.0";
        string[] candidates =
        [
            Path.Combine(repoRoot, "bin", "Debug",   tfm, "win10-x64", "LocalAIAssistant.Ui.Maui.exe")
          , Path.Combine(repoRoot, "bin", "Debug",   tfm,               "LocalAIAssistant.Ui.Maui.exe")
          , Path.Combine(repoRoot, "bin", "Release",  tfm, "win10-x64", "LocalAIAssistant.Ui.Maui.exe")
          , Path.Combine(repoRoot, "bin", "Release",  tfm,               "LocalAIAssistant.Ui.Maui.exe")
        ];

        var found = candidates.FirstOrDefault(File.Exists);
        if (found is not null) return found;

        throw new FileNotFoundException(
            $"Could not find LocalAIAssistant.Ui.Maui.exe under {repoRoot}. " +
            "Build the Windows target first: dotnet build -f net9.0-windows10.0.19041.0 " +
            "or set LAA_EXE_PATH.");
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "LocalAIAssistant.Ui.Maui.csproj")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException(
            "Could not locate the repo root. Set LAA_EXE_PATH as a fallback.");
    }

    public void Dispose()
    {
        try { App?.Close(); }  catch { /* best-effort */ }
        try { Automation?.Dispose(); } catch { }
    }
}
