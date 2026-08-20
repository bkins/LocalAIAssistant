namespace LocalAIAssistant.Core.Tts;

public static class TtsProviderResolver
{
    public static string Resolve(string? azureKey, string? elevenLabsKey)
    {
        if (azureKey.HasValue())
            return TtsProvider.Azure;

        if (elevenLabsKey.HasValue())
            return TtsProvider.ElevenLabs;

        return TtsProvider.Maui;
    }
}
