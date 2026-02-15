using CP.Client.Core.Common.ConectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.Tasks;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Tasks;

public class TaskApiClientFactory : BaseHttpClient, ITaskApiClientFactory
{
    private readonly IHttpClientFactory       _httpFactory;
    private readonly ApiEnvironmentDescriptor _env;

    public TaskApiClientFactory(IHttpClientFactory         httpFactory
                                , ApiEnvironmentDescriptor env)
    {
        _httpFactory  = httpFactory;
        _env          = env;
    }

    public ITaskApiClient Create()
    {
        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(_env.BaseUrl);
        http.Timeout     = TimeSpan.FromSeconds(Timeout);

        return new TaskApiClient(http);
    }
}
