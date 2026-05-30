namespace LocalAIAssistant.Services.Health;

// Configuration for the local Health Connect HTTP gateway.
// Binds from IConfiguration section "HealthGateway" — see appsettings.json for key names.
// To change at runtime, load a JSON file from FileSystem.AppDataDirectory (OllamaConfig pattern).
public sealed class HealthGatewayConfig
{
    public int    Port         { get; set; } = 5050;
    public string SharedSecret { get; set; } = string.Empty;
}
