using LocalAIAssistant.Core.Tts;
using LocalAIAssistant.Data;
using Microsoft.Maui.Media;

namespace LocalAIAssistant.Services;

public sealed class MauiTtsService : ITtsService
{
    private const string EnabledPrefKey   = StringConsts.TtsEnabledPrefKey;
    private const string VoiceNamePrefKey = StringConsts.TtsPreferredVoiceNamePrefKey;

    private CancellationTokenSource? _speakCts;
    private bool                     _isTtsAvailable;
    private IReadOnlyList<Locale>?   _cachedLocales;

    public bool IsTtsAvailable => _isTtsAvailable;

    public bool IsEnabled
    {
        get => Preferences.Default.Get(EnabledPrefKey, false);
        set => Preferences.Default.Set(EnabledPrefKey, value);
    }

    public string? PreferredVoiceName
    {
        get
        {
            var stored = Preferences.Default.Get(VoiceNamePrefKey, string.Empty);
            return string.IsNullOrEmpty(stored) ? null : stored;
        }
        set => Preferences.Default.Set(VoiceNamePrefKey, value ?? string.Empty);
    }

    public MauiTtsService()
    {
        _isTtsAvailable = ProbeAvailability();
    }

    private static bool ProbeAvailability()
    {
        try
        {
            _ = TextToSpeech.Default;
            return true;
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException)
        {
            return false;
        }
    }

    public async Task SpeakAsync(string text, CancellationToken ct = default)
    {
        if (!_isTtsAvailable || !IsEnabled || string.IsNullOrWhiteSpace(text))
            return;

        // Stop any in-progress speech before starting new utterance
        await StopAsync();

        _speakCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            var options = BuildSpeechOptions();
            await TextToSpeech.Default.SpeakAsync(text, options, _speakCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when StopAsync cancels the token
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException)
        {
            _isTtsAvailable = false;
        }
        finally
        {
            _speakCts?.Dispose();
            _speakCts = null;
        }
    }

    public async Task StopAsync()
    {
        var cts = _speakCts;
        if (cts is null) return;

        await cts.CancelAsync();
        cts.Dispose();

        // Only null out if it hasn't been replaced by a concurrent SpeakAsync
        if (ReferenceEquals(_speakCts, cts))
            _speakCts = null;
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync()
    {
        if (!_isTtsAvailable)
            return Array.Empty<VoiceInfo>();

        try
        {
            var locales = await TextToSpeech.GetLocalesAsync();
            _cachedLocales = locales.ToList();

            return _cachedLocales
                   .Select(locale => new VoiceInfo(locale.Name, locale.Language, locale.Country))
                   .OrderBy(voice => voice.Language)
                   .ThenBy(voice => voice.Name)
                   .ToList();
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException)
        {
            _isTtsAvailable = false;
            return Array.Empty<VoiceInfo>();
        }
    }

    private SpeechOptions BuildSpeechOptions()
    {
        var options = new SpeechOptions { Volume = 1.0f, Pitch = 1.0f };

        if (_cachedLocales is { Count: > 0 } && PreferredVoiceName is { } preferred)
        {
            var locale = _cachedLocales.FirstOrDefault(l => l.Name == preferred);
            if (locale is not null)
                options.Locale = locale;
        }

        return options;
    }
}
