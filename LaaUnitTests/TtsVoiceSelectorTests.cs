using LocalAIAssistant.Core.Tts;
using Xunit;

namespace LaaUnitTests;

public class TtsVoiceSelectorTests
{
    private static readonly IReadOnlyList<VoiceInfo> SampleVoices = new[]
    {
        new VoiceInfo("Microsoft Zira Desktop",   "en", "US")
      , new VoiceInfo("Microsoft David Desktop",  "en", "US")
      , new VoiceInfo("Microsoft Hazel Desktop",  "en", "GB")
      , new VoiceInfo("Microsoft Raul Desktop",   "es", "MX")
      , new VoiceInfo("Microsoft Helena Desktop", "es", "ES")
    };

    [Fact]
    public void SelectVoice_ReturnsNull_WhenVoicesEmpty()
    {
        var result = VoiceSelector.SelectVoice(Array.Empty<VoiceInfo>(), null, null);

        Assert.Null(result);
    }

    [Fact]
    public void SelectVoice_ReturnsPreferredVoice_WhenNameMatches()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, "Microsoft Hazel Desktop", "en");

        Assert.NotNull(result);
        Assert.Equal("Microsoft Hazel Desktop", result.Name);
    }

    [Fact]
    public void SelectVoice_FallsBackToLanguageMatch_WhenPreferredNameNotFound()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, "Unknown Voice", "es");

        Assert.NotNull(result);
        Assert.Equal("es", result.Language);
    }

    [Fact]
    public void SelectVoice_FallsBackToFirstVoice_WhenNoLanguageMatch()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, null, "ja");

        Assert.NotNull(result);
        Assert.Equal(SampleVoices[0].Name, result.Name);
    }

    [Fact]
    public void SelectVoice_ReturnsFirstVoice_WhenNoPreferenceProvided()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, null, null);

        Assert.NotNull(result);
        Assert.Equal(SampleVoices[0].Name, result.Name);
    }

    [Fact]
    public void SelectVoice_IsCaseInsensitiveForPreferredName()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, "MICROSOFT ZIRA DESKTOP", null);

        Assert.NotNull(result);
        Assert.Equal("Microsoft Zira Desktop", result.Name);
    }

    [Fact]
    public void SelectVoice_IsCaseInsensitiveForLanguageCode()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, null, "EN");

        Assert.NotNull(result);
        Assert.Equal("en", result.Language);
    }

    [Fact]
    public void SelectVoice_PreferredNameTakesPriorityOverLanguageMatch()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, "Microsoft Raul Desktop", "en");

        Assert.NotNull(result);
        Assert.Equal("Microsoft Raul Desktop", result.Name);
    }

    [Fact]
    public void SelectVoice_ReturnsNull_WhenVoicesIsEmpty_EvenWithPreference()
    {
        var result = VoiceSelector.SelectVoice(Array.Empty<VoiceInfo>(), "Microsoft Zira Desktop", "en");

        Assert.Null(result);
    }

    [Fact]
    public void SelectVoice_ReturnsFirstLanguageMatch_WhenMultipleShareSameLanguage()
    {
        var result = VoiceSelector.SelectVoice(SampleVoices, "No Match", "en");

        Assert.NotNull(result);
        Assert.Equal("en",                     result.Language);
        Assert.Equal("Microsoft Zira Desktop", result.Name);
    }
}
