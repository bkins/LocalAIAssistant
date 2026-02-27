using System.Net.Http.Json;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Core.Environment.Models;

namespace LocalAIAssistant.Core.Environment;

public sealed class StartupHandshakeService
{
    private readonly EnvironmentHandshakeState _state;
    private readonly ICognitivePlatformClientFactory _cognitivePlatformClientFactory;

    public StartupHandshakeService( EnvironmentHandshakeState       state
                                  , ICognitivePlatformClientFactory cognitivePlatformClientFactory )
    {
        _state                          = state;
        _cognitivePlatformClientFactory = cognitivePlatformClientFactory;
    }

    public async Task RunAsync(string clientEnvironment, CancellationToken ct = default)
    {
        try
        {
            // This shape depends on your API response model.
            // If your /system/environment returns { environmentName: "DEV", ... }
            var cpClient = _cognitivePlatformClientFactory.Create();
            var api      = await cpClient.SystemEnvironmentAsync(ct);
        

            var apiEnv = api.EnvironmentName ?? "UNKNOWN";
            var result = EnvironmentHandshakePolicy.Evaluate(clientEnvironment, apiEnv);
        
            _state.Set(result);
        }
        catch (Exception e)
        {
            // If there's an error during the handshake, set the state to Failed with details.
            _state.Set(EnvironmentHandshakePolicy.Failed(clientEnvironment, apiEnv: "UNKNOWN", e));
        }
        
    }
}