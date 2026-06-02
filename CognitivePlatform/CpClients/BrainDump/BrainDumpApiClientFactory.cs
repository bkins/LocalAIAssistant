using LocalAIAssistant.Core.BrainDump;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.CognitivePlatform.CpClients.BrainDump;

public class BrainDumpApiClientFactory : BaseHttpClient, IBrainDumpApiClientFactory
{
    private readonly IHttpClientFactory       _httpFactory;
    private readonly ApiEnvironmentDescriptor _env;

    public BrainDumpApiClientFactory( IHttpClientFactory         httpFactory
                                    , ApiEnvironmentDescriptor   env )
    {
        _httpFactory = httpFactory;
        _env         = env;
    }

    public IBrainDumpApiClient Create()
    {
        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(_env.BaseUrl);
        http.Timeout     = TimeSpan.FromSeconds(Timeout);

        return new BrainDumpApiClient(http);
    }
}
