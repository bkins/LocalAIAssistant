using LocalAIAssistant.Core.Notifications;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace LocalAIAssistant.Services;

public sealed class PluginLocalNotificationScheduler : INotificationScheduler
{
    private readonly INotificationService _notificationService;

    public PluginLocalNotificationScheduler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task<bool> AreNotificationsEnabledAsync()
        => _notificationService.AreNotificationsEnabled(new NotificationPermission());

    public void CancelAll()
        => _notificationService.CancelAll();

    public async Task ScheduleAsync(int      notificationId
                                  , string   title
                                  , string   description
                                  , DateTime notifyTime
                                  , string   channelId)
    {
        await _notificationService.Show(new NotificationRequest
              {
                  NotificationId = notificationId
                , Title          = title
                , Description    = description
                , Schedule       = new NotificationRequestSchedule { NotifyTime = notifyTime }
                , Android        = new AndroidOptions { ChannelId = channelId }
              });
    }
}
