namespace LocalAIAssistant.CognitivePlatform.CpClients.Notifications;

public interface INotificationApiClient
{
    Task<NotificationSchedule> GetNotificationScheduleAsync(DateTimeOffset         from
                                                           , CancellationToken ct = default);
}
