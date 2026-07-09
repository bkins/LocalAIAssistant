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
    private readonly SemaphoreSlim _speakLock = new(1, 1);

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

        await _speakLock.WaitAsync(ct);
        try
        {
            // Cancel any in-progress speech.
            var oldCts = _speakCts;
            if (oldCts is not null)
            {
                await oldCts.CancelAsync();
                oldCts.Dispose();
                _speakCts = null;
            }

            // Brief delay for the platform TTS engine to fully release audio resources.
            // Without this, Android may clip the first ~100ms of the new utterance.
            await Task.Delay(50, ct);

            _speakCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var cleanText = TtsMarkdownCleaner.StripMarkdown(text);
            var options = BuildSpeechOptions();
            await TextToSpeech.Default.SpeakAsync(cleanText, options, _speakCts.Token);
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
            _speakLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _speakLock.WaitAsync();
        try
        {
            var cts = _speakCts;
            if (cts is null) return;

            await cts.CancelAsync();
            cts.Dispose();
            _speakCts = null;
        }
        finally
        {
            _speakLock.Release();
        }
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync()
    {
        if ( ! _isTtsAvailable) return Array.Empty<VoiceInfo>();

        try
        {
            // Android TTS init can block the calling thread; run on thread pool to keep the UI responsive.
            var locales = await Task.Run(async () => await TextToSpeech.GetLocalesAsync());
            _cachedLocales = locales.ToList();

            return _cachedLocales.Select(locale => new VoiceInfo(locale.Name
                                                               , locale.Language
                                                               , locale.Country))
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
