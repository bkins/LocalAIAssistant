using LocalAIAssistant.Services;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

public class KnowledgeClientFactory : BaseHttpClient, IKnowledgeClientFactory
{
    private readonly IHttpClientFactory       _httpFactory;
    private readonly ApiEnvironmentDescriptor _env;

    public KnowledgeClientFactory(IHttpClientFactory       httpFactory
                                , ApiEnvironmentDescriptor env)
    {
        _httpFactory  = httpFactory;
        _env          = env;
    }

    public IKnowledgeApiClient Create()
    {
        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(_env.BaseUrl);
        http.Timeout     = TimeSpan.FromSeconds(Timeout);

        return new KnowledgeApiClient(http);
    }
}
