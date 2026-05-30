using LocalAIAssistant.Core.Notifications;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Notifications;

public interface INotificationApiClientFactory
{
    INotificationApiClient Create();
}
