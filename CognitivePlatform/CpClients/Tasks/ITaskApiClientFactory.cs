using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Tasks;

public interface ITaskApiClientFactory
{
    ITaskApiClient Create();
}
