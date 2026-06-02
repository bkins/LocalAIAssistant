using LocalAIAssistant.Core.BrainDump;

namespace LocalAIAssistant.CognitivePlatform.CpClients.BrainDump;

public interface IBrainDumpApiClientFactory
{
    IBrainDumpApiClient Create();
}
