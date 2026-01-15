using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConectivityToApi;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

public class CognitivePlatformClientFactory : BaseHttpClient, ICognitivePlatformClientFactory
{
    private readonly IHttpClientFactory    _httpFactory;
    private readonly ApiEnvironmentService _env;
    private readonly IConnectivityReporter _connectivity;

    public CognitivePlatformClientFactory(IHttpClientFactory     httpFactory
                                         , ApiEnvironmentService env
                                         , IConnectivityReporter connectivity)
    {
        _httpFactory  = httpFactory;
        _env          = env;
        _connectivity = connectivity;
    }

    public ICognitivePlatformClient Create()
    {
        if (_env.IsInitialized
                .Not())
        {
            throw new InvalidOperationException("API environment has not been selected yet.");
        }
        
        var client = _httpFactory.CreateClient(HttpClientNames.CpApi);

        client.BaseAddress = new Uri(_env.BaseUrl);
        client.Timeout     = TimeSpan.FromSeconds(Timeout);

        return new CognitivePlatformClient(client, _connectivity);
    }
}
