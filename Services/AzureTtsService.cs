using LocalAIAssistant.Core.Tts;
using LocalAIAssistant.Data;
using SpeechSdk = Microsoft.CognitiveServices.Speech;

namespace LocalAIAssistant.Services;

public sealed class AzureTtsService : ITtsService
{
    private readonly MauiTtsService _fallback;

    private SpeechSdk.SpeechSynthesizer? _currentSynthesizer;

    public AzureTtsService(MauiTtsService fallback)
    {
        _fallback = fallback;
    }

    private static string? ActiveKey
        => Preferences.Default.Get(StringConsts.TtsAzureKeyPrefKey, string.Empty) is { Length: > 0 } key
               ? key
               : null;

    private static string ActiveRegion
        => Preferences.Default.Get(StringConsts.TtsAzureRegionPrefKey, "eastus");

    public bool IsTtsAvailable => !string.IsNullOrEmpty(ActiveKey);

    public bool IsEnabled
    {
        get => Preferences.Default.Get(StringConsts.TtsEnabledPrefKey, false);
        set => Preferences.Default.Set(StringConsts.TtsEnabledPrefKey, value);
    }

    public string? PreferredVoiceName
    {
        get
        {
            var stored = Preferences.Default.Get(StringConsts.TtsPreferredVoiceNamePrefKey, string.Empty);
            return string.IsNullOrEmpty(stored) ? null : stored;
        }
        set => Preferences.Default.Set(StringConsts.TtsPreferredVoiceNamePrefKey, value ?? string.Empty);
    }

    public async Task SpeakAsync(string text, CancellationToken ct = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text))
            return;

        var key = ActiveKey;
        if (string.IsNullOrEmpty(key))
        {
            await _fallback.SpeakAsync(text, ct);
            return;
        }

        await StopAsync();

            // Brief delay for the TTS engine to fully release audio resources.
            await Task.Delay(50, ct);

        try
        {
            var config = SpeechSdk.SpeechConfig.FromSubscription(key, ActiveRegion);
            if (!string.IsNullOrEmpty(PreferredVoiceName))
                config.SpeechSynthesisVoiceName = PreferredVoiceName;

            _currentSynthesizer = new SpeechSdk.SpeechSynthesizer(config);

            await using var cancelRegistration = ct.Register(
                () => _ = _currentSynthesizer?.StopSpeakingAsync());

            var result = await _currentSynthesizer.SpeakTextAsync(TtsMarkdownCleaner.StripMarkdown(text));

            if (result.Reason == SpeechSdk.ResultReason.Canceled)
            {
                var details = SpeechSdk.SpeechSynthesisCancellationDetails.FromResult(result);
                if (details.ErrorCode != SpeechSdk.CancellationErrorCode.NoError)
                    await _fallback.SpeakAsync(text, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when StopAsync cancels the token
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            await _fallback.SpeakAsync(text, ct);
        }
        finally
        {
            _currentSynthesizer?.Dispose();
            _currentSynthesizer = null;
        }
    }

    public async Task StopAsync()
    {
        var synthesizer = _currentSynthesizer;
        if (synthesizer is null) return;

        try
        {
            await synthesizer.StopSpeakingAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
        }
        finally
        {
            synthesizer.Dispose();
            if (ReferenceEquals(_currentSynthesizer, synthesizer))
                _currentSynthesizer = null;
        }
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync()
    {
        var key = ActiveKey;
        if (string.IsNullOrEmpty(key))
            return await _fallback.GetVoicesAsync();

        try
        {
            var config = SpeechSdk.SpeechConfig.FromSubscription(key, ActiveRegion);
            using var synthesizer = new SpeechSdk.SpeechSynthesizer(config);
            var result = await synthesizer.GetVoicesAsync();

            if (result.Reason != SpeechSdk.ResultReason.VoicesListRetrieved)
                return await _fallback.GetVoicesAsync();

            return result.Voices
                   .Select(voice => new VoiceInfo( voice.ShortName
                                                  , ExtractLanguage(voice.Locale)
                                                  , ExtractCountry(voice.Locale)))
                   .OrderBy(voice => voice.Language)
                   .ThenBy(voice => voice.Name)
                   .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            return await _fallback.GetVoicesAsync();
        }
    }

    private static string ExtractLanguage(string locale)
    {
        var dashIndex = locale.IndexOf('-');
        return dashIndex > 0 ? locale[..dashIndex] : locale;
    }

    private static string ExtractCountry(string locale)
    {
        var parts = locale.Split('-');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }
}
