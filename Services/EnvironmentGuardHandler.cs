using CP.Client.Core.Avails;

namespace LocalAIAssistant.Services;

public sealed class EnvironmentGuardHandler : DelegatingHandler
{
    private readonly ApiEnvironmentService _env;

    public EnvironmentGuardHandler(ApiEnvironmentService env)
    {
        _env = env;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request
                                                          , CancellationToken cancellationToken)
    {
        if (_env.IsInitialized.Not())
        {
            throw new InvalidOperationException("API environment is not initialized. Request blocked.");
        }

        var expectedBase = _env.BaseUrl.TrimEnd('/');
        var actual       = request.RequestUri?.GetLeftPart(UriPartial.Authority);

        if (actual is null 
         || actual.StartsWith(expectedBase, StringComparison.OrdinalIgnoreCase)
                  .Not())
        {
            throw new InvalidOperationException($"Environment mismatch detected. "
                                              + $"Expected base '{expectedBase}', but request was '{request.RequestUri}'.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}