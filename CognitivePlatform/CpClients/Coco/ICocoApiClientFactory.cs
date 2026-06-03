using LocalAIAssistant.Core.Coco;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Coco;

public interface ICocoApiClientFactory
{
    ICocoApiClient Create();
}
