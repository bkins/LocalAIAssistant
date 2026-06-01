using LocalAIAssistant.Core.ConversationHistory;
using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Core.Personality;
using LocalAIAssistant.Core.Tts;
using LocalAIAssistant.Data;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIAssistant.Views;

public partial class DebugStartupPage : ContentPage
{
    public DebugStartupPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RunDiagnosticsAsync();
    }

    private async Task RunDiagnosticsAsync()
    {
        var services = IPlatformApplication.Current?.Services
                    ?? Application.Current?.Handler?.MauiContext?.Services;
        if (services == null)
        {
            StatusLabel.Text  = "Error: Could not resolve services. Try tapping the page first.";
            Spinner.IsRunning = false;
            return;
        }

        var log = new System.Text.StringBuilder();
        var sw  = System.Diagnostics.Stopwatch.StartNew();

        async Task Step(string name, Func<Task> action)
        {
            StatusLabel.Text = $"Running: {name}";

            var start = sw.ElapsedMilliseconds;
            try
            {
                await Task.Yield();
                await action();
                var elapsed = sw.ElapsedMilliseconds - start;
                log.AppendLine($"✅ {name}: {elapsed}ms");
            }
            catch (Exception ex)
            {
                var elapsed = sw.ElapsedMilliseconds - start;
                log.AppendLine($"❌ {name}: {elapsed}ms — {ex.GetType().Name}: {ex.Message}");
            }

            LogLabel.Text = log.ToString();
        }

        // ── App.OnStart steps ──────────────────────────────────────────────────

        await Step("ApiHealthService.InitializeAsync", async () =>
        {
            var svc = services.GetService<ApiHealthService>();
            if (svc != null)
                await svc.InitializeAsync();
        });

        await Step("StartupHandshakeService.RunAsync", async () =>
        {
            var svc = services.GetService<StartupHandshakeService>();
            if (svc != null)
                await svc.RunAsync(BuildEnvironment.Name);
        });

        // ── AppShell.OnAppearing step ──────────────────────────────────────────

        await Step("AppShellMasterViewModel.InitializeAsync", async () =>
        {
            var svc = services.GetService<AppShellMasterViewModel>();
            if (svc != null)
                await svc.InitializeAsync();
        });

        // ── MainPage.OnAppearing steps ─────────────────────────────────────────

        ChatViewModel? chatVm = null;

        await Step("Resolve ChatViewModel", async () =>
        {
            chatVm = services.GetService<ChatViewModel>();
            await Task.CompletedTask;
        });

        await Step("ChatViewModel.InitializeAsync", async () =>
        {
            if (chatVm != null)
                await chatVm.InitializeAsync();
        });

        // ── Isolated sub-step checks ───────────────────────────────────────────
        // These run after InitializeAsync so some may be warm. A step that is
        // still slow here is inherently slow; a fast step was the culprit inside
        // InitializeAsync on first call.

        await Step("ITtsService.GetVoicesAsync", async () =>
        {
            var svc = services.GetService<ITtsService>();
            if (svc != null)
                await svc.GetVoicesAsync();
        });

        await Step("IPersonalityApiClient.GetPersonalitiesAsync", async () =>
        {
            var svc = services.GetService<IPersonalityApiClient>();
            if (svc != null)
                await svc.GetPersonalitiesAsync();
        });

        await Step("IConversationHistoryClient.GetHistoryAsync", async () =>
        {
            var svc            = services.GetService<IConversationHistoryClient>();
            var conversationId = Preferences.Get(StringConsts.ActiveConversationIdKey, Guid.NewGuid().ToString());
            if (svc != null)
                await svc.GetHistoryAsync(conversationId);
        });

        await Step("IConversationMemory.LoadShortTermAsync", async () =>
        {
            var svc = services.GetService<IConversationMemory>();
            if (svc != null)
                await svc.LoadShortTermAsync();
        });

        await Step("IOfflineQueueService.ResetProcessingItemsAsync", async () =>
        {
            var svc = services.GetService<IOfflineQueueService>();
            if (svc != null)
                await svc.ResetProcessingItemsAsync();
        });

        // ── App.OnResume step ──────────────────────────────────────────────────

        await Step("NotificationSyncService.SyncAsync", async () =>
        {
            var svc = services.GetService<NotificationSyncService>();
            if (svc != null)
                await svc.SyncAsync();
        });

        sw.Stop();

        log.AppendLine();
        log.AppendLine($"Total elapsed: {sw.ElapsedMilliseconds}ms");

        LogLabel.Text         = log.ToString();
        StatusLabel.Text      = "Done.";
        Spinner.IsRunning     = false;
        GoToAppButton.IsVisible = true;
    }

    private async void OnGoToAppClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(animated: false);
    }
}
