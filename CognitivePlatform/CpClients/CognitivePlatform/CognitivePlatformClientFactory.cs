using CP.Client.Core.Common.ConectivityToApi;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Logging;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

public class CognitivePlatformClientFactory : BaseHttpClient, ICognitivePlatformClientFactory
{
    private readonly IHttpClientFactory       _httpFactory;
    private readonly ApiEnvironmentDescriptor _env;
    private readonly IConnectivityReporter    _connectivity;
    private readonly ILoggingService          _logger;

    public CognitivePlatformClientFactory (IHttpClientFactory       httpFactory
                                         , ApiEnvironmentDescriptor env
                                         , IConnectivityReporter    connectivity
                                         , ILoggingService          logger)
    {
        _httpFactory  = httpFactory;
        _env          = env;
        _connectivity = connectivity;
        _logger       = logger;
    }

    public ICognitivePlatformClient Create()
    {
        var client = _httpFactory.CreateClient(HttpClientNames.CpApi);

        client.BaseAddress = new Uri(_env.BaseUrl);
        client.Timeout     = TimeSpan.FromSeconds(Timeout);
        
        var cpClient = new CognitivePlatformClient(client, _connectivity, _logger);
        
        _logger.LogInformation($"CP Client created: {cpClient}");
        
        return cpClient;
    }
}
