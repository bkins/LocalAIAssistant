namespace LocalAIAssistant.Core.Notifications;

public interface INotificationApiClient
{
    Task<NotificationSchedule> GetNotificationScheduleAsync(DateTimeOffset         from
                                                           , CancellationToken ct = default);
}
