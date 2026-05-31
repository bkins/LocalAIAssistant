namespace LocalAIAssistant.Core.Tts;

public static class TtsProviderResolver
{
    public static string Resolve(string? azureKey, string? elevenLabsKey)
    {
        if (!string.IsNullOrWhiteSpace(azureKey))
            return TtsProvider.Azure;

        if (!string.IsNullOrWhiteSpace(elevenLabsKey))
            return TtsProvider.ElevenLabs;

        return TtsProvider.Maui;
    }
}
