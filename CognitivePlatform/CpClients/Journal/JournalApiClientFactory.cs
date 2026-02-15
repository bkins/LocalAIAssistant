using LocalAIAssistant.Services;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Journal;

public class JournalApiClientFactory : BaseHttpClient, IJournalApiClientFactory
{
    private readonly IHttpClientFactory       _httpFactory;
    private readonly ApiEnvironmentDescriptor _env;

    public JournalApiClientFactory(IHttpClientFactory      httpFactory
                                , ApiEnvironmentDescriptor env)
    {
        _httpFactory  = httpFactory;
        _env          = env;
    }

    public IJournalApiClient Create()
    {
        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(_env.BaseUrl);
        http.Timeout     = TimeSpan.FromSeconds(Timeout);

        return new JournalApiClient(http);
    }
}
