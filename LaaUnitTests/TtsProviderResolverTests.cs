using LocalAIAssistant.Core.Tts;

namespace LaaUnitTests;

public class TtsProviderResolverTests
{
    [Fact]
    public void Resolve_ReturnsMaui_WhenBothKeysEmpty()
    {
        var result = TtsProviderResolver.Resolve(string.Empty, string.Empty);

        Assert.Equal(TtsProvider.Maui, result);
    }

    [Fact]
    public void Resolve_ReturnsMaui_WhenBothKeysNull()
    {
        var result = TtsProviderResolver.Resolve(null, null);

        Assert.Equal(TtsProvider.Maui, result);
    }

    [Fact]
    public void Resolve_ReturnsMaui_WhenBothKeysWhitespace()
    {
        var result = TtsProviderResolver.Resolve("   ", "   ");

        Assert.Equal(TtsProvider.Maui, result);
    }

    [Fact]
    public void Resolve_ReturnsAzure_WhenAzureKeyIsSet()
    {
        var result = TtsProviderResolver.Resolve("my-azure-key", null);

        Assert.Equal(TtsProvider.Azure, result);
    }

    [Fact]
    public void Resolve_ReturnsElevenLabs_WhenOnlyElevenLabsKeyIsSet()
    {
        var result = TtsProviderResolver.Resolve(null, "my-elevenlabs-key");

        Assert.Equal(TtsProvider.ElevenLabs, result);
    }

    [Fact]
    public void Resolve_PrefersAzure_WhenBothKeysAreSet()
    {
        var result = TtsProviderResolver.Resolve("azure-key", "elevenlabs-key");

        Assert.Equal(TtsProvider.Azure, result);
    }

    [Fact]
    public void Resolve_ReturnsElevenLabs_WhenAzureKeyEmptyAndElevenLabsKeySet()
    {
        var result = TtsProviderResolver.Resolve(string.Empty, "elevenlabs-key");

        Assert.Equal(TtsProvider.ElevenLabs, result);
    }

    [Fact]
    public void Resolve_ReturnsMaui_WhenAzureKeyWhitespaceAndElevenLabsKeyEmpty()
    {
        var result = TtsProviderResolver.Resolve("  ", string.Empty);

        Assert.Equal(TtsProvider.Maui, result);
    }
}
