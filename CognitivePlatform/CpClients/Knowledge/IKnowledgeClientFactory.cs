using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

public interface IKnowledgeClientFactory
{
    IKnowledgeApiClient Create();
}
