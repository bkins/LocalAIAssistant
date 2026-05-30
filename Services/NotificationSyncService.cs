using LocalAIAssistant.CognitivePlatform.CpClients.Notifications;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace LocalAIAssistant.Services;

public sealed class NotificationSyncService
{
    private readonly INotificationApiClientFactory _clientFactory;
    private readonly INotificationService          _notificationService;
    private readonly ILoggingService               _loggingService;

    public NotificationSyncService(INotificationApiClientFactory clientFactory
                                 , INotificationService          notificationService
                                 , ILoggingService               loggingService)
    {
        _clientFactory       = clientFactory;
        _notificationService = notificationService;
        _loggingService      = loggingService;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        try
        {
            if (!await _notificationService.AreNotificationsEnabled(new NotificationPermission()))
                return;

            var client   = _clientFactory.Create();
            var schedule = await client.GetNotificationScheduleAsync(DateTimeOffset.Now, ct);

            _notificationService.CancelAll();

            foreach (var notification in schedule.Notifications)
            {
                await _notificationService.Show(new NotificationRequest
                {
                    NotificationId = GetStableHashCode(notification.ExternalId)
                  , Title          = notification.Title
                  , Description    = notification.Body
                  , Schedule       = new NotificationRequestSchedule
                                     {
                                         NotifyTime = notification.FireAt.LocalDateTime
                                     }
                  , Android        = new AndroidOptions
                                     {
                                         ChannelId = "cp-reminders"
                                     }
                });
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
