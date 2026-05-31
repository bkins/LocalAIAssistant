namespace LocalAIAssistant.Core.Tts;

public sealed class TtsSettings
{
    public string  Provider         { get; set; } = TtsProvider.Maui;
    public string? AzureKey         { get; set; }
    public string? AzureRegion      { get; set; }
    public string? ElevenLabsApiKey { get; set; }
}

public static class TtsProvider
{
    public const string Maui       = "Maui";
    public const string Azure      = "Azure";
    public const string ElevenLabs = "ElevenLabs";
}
