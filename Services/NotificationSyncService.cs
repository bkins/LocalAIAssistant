using LocalAIAssistant.CognitivePlatform.CpClients.Notifications;
using LocalAIAssistant.Core.Notifications;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.Services;

public sealed class NotificationSyncService
{
    private readonly INotificationApiClientFactory _clientFactory;
    private readonly INotificationScheduler        _scheduler;
    private readonly ILoggingService               _loggingService;

    public NotificationSyncService(INotificationApiClientFactory clientFactory
                                 , INotificationScheduler        scheduler
                                 , ILoggingService               loggingService)
    {
        _clientFactory  = clientFactory;
        _scheduler      = scheduler;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Fetches the upcoming schedule from the CP API and replaces the current set of
    /// OS-registered CP notifications with the new one.
    /// </summary>
    /// <remarks>
    /// <para><b>API-unreachable handling:</b> Any network or HTTP error thrown by
    /// <see cref="INotificationApiClient"/> propagates into this method's catch block
    /// and is logged; <see cref="INotificationScheduler.CancelAll"/> is never reached,
    /// so the existing OS schedule is preserved unchanged.
    /// Only <see cref="OperationCanceledException"/> escapes this method.</para>
    ///
    /// <para><b>Idempotency:</b> Calling <c>SyncAsync</c> twice produces the same result
    /// as calling it once. <c>CancelAll()</c> is called before every re-schedule, and
    /// each notification's OS identifier is derived deterministically from its
    /// <c>ExternalId</c> via the stable hash — duplicate registration is structurally
    /// impossible.</para>
    ///
    /// <para><b>Revoked permission (Android 13+):</b> If the user has denied or revoked
    /// notification permission, <c>SyncAsync</c> returns immediately without modifying
    /// the schedule. Android automatically removes previously-scheduled notifications
    /// from the tray when permission is revoked, so no manual cancel is needed.</para>
    ///
    /// <para><b>Server-side guard rules</b> (quiet hours, max-per-day, min-gap) are
    /// configured in the CP API's <c>appsettings.json</c> under the
    /// <c>Notifications</c> section. Defaults: MaxPerDay=5, MinGapMinutes=90,
    /// QuietHoursStart=22, QuietHoursEnd=7.</para>
    /// </remarks>
    public async Task SyncAsync(CancellationToken ct = default)
    {
        try
        {
            if (!await _scheduler.AreNotificationsEnabledAsync())
                return;

            var client   = _clientFactory.Create();
            var schedule = await client.GetNotificationScheduleAsync(DateTimeOffset.Now, ct);

            _scheduler.CancelAll();

            foreach (var notification in schedule.Notifications)
            {
                await _scheduler.ScheduleAsync(GetStableHashCode(notification.ExternalId)
                                             , notification.Title
                                             , notification.Body
                                             , notification.FireAt.LocalDateTime
                                             , "cp-reminders");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _loggingService.LogError(ex, "Notification sync failed; keeping existing schedule", Category.App);
        }
    }

    private static int GetStableHashCode(string text)
    {
        unchecked
        {
            var hash = 17;
            foreach (var character in text)
                hash = hash * 31 + character;
            return hash;
        }
    }
}
