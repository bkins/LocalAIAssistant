namespace LocalAIAssistant.Core.Notifications;

/// <summary>
/// Abstracts the platform notification delivery layer (Plugin.LocalNotification)
/// so that code depending on it can be tested without MAUI infrastructure.
/// </summary>
public interface INotificationScheduler
{
    Task<bool> AreNotificationsEnabledAsync();
    void       CancelAll();
    Task       ScheduleAsync(int      notificationId
                           , string   title
                           , string   description
                           , DateTime notifyTime
                           , string   channelId);
}
