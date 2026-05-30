namespace LocalAIAssistant.Services.FileSync;

public sealed class FileGatewayConfig
{
    public int      Port         { get; init; } = 5051;
    public string   SharedSecret { get; init; } = string.Empty;
    public string[] AllowedPaths { get; init; } = [];
}
