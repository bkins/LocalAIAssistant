using LocalAIAssistant.Core.Coco;
using LocalAIAssistant.Data;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Coco;

public sealed class CocoApiClientFactory : ICocoApiClientFactory
{
    private readonly IHttpClientFactory _httpFactory;

    public CocoApiClientFactory(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public ICocoApiClient Create()
    {
        var baseUrl = Preferences.Default.Get(StringConsts.CocoBaseUrlPrefKey
                                            , StringConsts.CocoDefaultBaseUrl);

        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.Timeout     = TimeSpan.FromSeconds(120);

        return new CocoApiClient(http);
    }
}
