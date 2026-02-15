using CP.Client.Core.Avails;

namespace LocalAIAssistant.Services;

public sealed class EnvironmentGuardHandler : DelegatingHandler
{
    private readonly string _expectedAuthority;
    private readonly string _envName;
    private readonly string _ollamaBaseUrl;

    public EnvironmentGuardHandler(ApiEnvironmentDescriptor env)
    {
        _envName           = env.Name;
        _expectedAuthority = new Uri(env.BaseUrl).GetLeftPart(UriPartial.Authority);
        _ollamaBaseUrl     = new Uri(env.OllamaUrl).GetLeftPart(UriPartial.Authority);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request
                                                         , CancellationToken  cancellationToken)
    {
        var actualAuthority = request.RequestUri
                                     ?.GetLeftPart(UriPartial.Authority);

        if (actualAuthority is null
         || HasMismatch(actualAuthority))
        {
            throw new InvalidOperationException($"Environment mismatch detected. "
                                              + $"App='{_envName}', Expected='{_expectedAuthority}', "
                                              + $"Actual='{request.RequestUri}'.");
        }

        return base.SendAsync(request, cancellationToken);
    }

    private bool HasMismatch(string actualAuthority)
    {
        var authorityDoesNotMatchExpected = string.Equals(actualAuthority
                                                        , _expectedAuthority
                                                        , StringComparison.OrdinalIgnoreCase)
                                                  .Not();
        var authorityIsNotOllama = string.Equals(actualAuthority
                                            , _ollamaBaseUrl
                                            , StringComparison.OrdinalIgnoreCase)
                                         .Not();
        
        return authorityDoesNotMatchExpected 
            && authorityIsNotOllama;
    }
}