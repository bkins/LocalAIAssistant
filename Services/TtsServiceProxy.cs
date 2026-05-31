using LocalAIAssistant.Core.Tts;
using LocalAIAssistant.Data;

namespace LocalAIAssistant.Services;

public sealed class TtsServiceProxy : ITtsService
{
    private readonly MauiTtsService        _maui;
    private readonly AzureTtsService      _azure;
    private readonly ElevenLabsTtsService _elevenLabs;

    public TtsServiceProxy( MauiTtsService        maui
                           , AzureTtsService      azure
                           , ElevenLabsTtsService elevenLabs)
    {
        _maui        = maui;
        _azure       = azure;
        _elevenLabs  = elevenLabs;
    }

    private ITtsService Active
    {
        get
        {
            var resolvedProvider = TtsProviderResolver.Resolve(
                Preferences.Default.Get(StringConsts.TtsAzureKeyPrefKey,      string.Empty)
              , Preferences.Default.Get(StringConsts.TtsElevenLabsKeyPrefKey, string.Empty));

            return resolvedProvider switch
            {
                TtsProvider.Azure      => _azure,
                TtsProvider.ElevenLabs => _elevenLabs,
                _                      => _maui
            };
        }
    }

    public bool IsTtsAvailable => _maui.IsTtsAvailable;

    public bool IsEnabled
    {
        get => Active.IsEnabled;
        set => Active.IsEnabled = value;
    }

    public string? PreferredVoiceName
    {
        get => Active.PreferredVoiceName;
        set => Active.PreferredVoiceName = value;
    }

    public Task SpeakAsync(string text, CancellationToken ct = default) => Active.SpeakAsync(text, ct);

    public Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync() => Active.GetVoicesAsync();

    public async Task StopAsync()
        => await Task.WhenAll(_maui.StopAsync(), _azure.StopAsync(), _elevenLabs.StopAsync());
}
