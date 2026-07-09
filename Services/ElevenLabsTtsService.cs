using System.Text.Json;
using System.Text.Json.Serialization;
using LocalAIAssistant.Core.Tts;
using LocalAIAssistant.Data;
using Plugin.Maui.Audio;
using TextEncoding = System.Text.Encoding;

namespace LocalAIAssistant.Services;

public sealed class ElevenLabsTtsService : ITtsService
{
    private const string VoicesEndpoint    = "/v1/voices";
    private const string TtsEndpointBase   = "/v1/text-to-speech";
    private const string DefaultModelId    = "eleven_multilingual_v2";
    private const string DefaultVoiceId    = "21m00Tcm4TlvDq8ikWAM"; // Rachel

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient     _httpClient;
    private readonly IAudioManager  _audioManager;
    private readonly MauiTtsService _fallback;

    private Dictionary<string, string> _nameToVoiceId = new(StringComparer.OrdinalIgnoreCase);
    private IAudioPlayer?              _currentPlayer;
    private MemoryStream?              _audioStream;

    public ElevenLabsTtsService(HttpClient httpClient, IAudioManager audioManager, MauiTtsService fallback)
    {
        _httpClient   = httpClient;
        _audioManager = audioManager;
        _fallback      = fallback;
    }

    private static string? ActiveKey
        => Preferences.Default.Get(StringConsts.TtsElevenLabsKeyPrefKey, string.Empty) is { Length: > 0 } key
               ? key
               : null;

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

        try
        {
            var voiceId = ResolveVoiceId(PreferredVoiceName);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{TtsEndpointBase}/{voiceId}");
            request.Headers.Add("xi-api-key", key);
            request.Headers.Add("Accept", "audio/mpeg");

            var cleanText = TtsMarkdownCleaner.StripMarkdown(text);
            var body = JsonSerializer.Serialize(new { text = cleanText, model_id = DefaultModelId });
            request.Content = new StringContent(body, TextEncoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var audioBytes = await response.Content.ReadAsByteArrayAsync(ct);

            _audioStream = new MemoryStream(audioBytes);
            _currentPlayer = _audioManager.CreatePlayer(_audioStream);
            _currentPlayer.Play();
        }
        catch (OperationCanceledException)
        {
            // Expected when StopAsync or the caller cancels
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            DisposeCurrentPlayer();
            await _fallback.SpeakAsync(text, ct);
        }
    }

    public Task StopAsync()
    {
        DisposeCurrentPlayer();
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync()
    {
        var key = ActiveKey;
        if (string.IsNullOrEmpty(key))
            return await _fallback.GetVoicesAsync();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, VoicesEndpoint);
            request.Headers.Add("xi-api-key", key);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json   = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<ElevenLabsVoicesResponse>(json, JsonOptions);

            if (parsed?.Voices is not { Count: > 0 })
                return Array.Empty<VoiceInfo>();

            _nameToVoiceId = parsed.Voices.ToDictionary(
                voice => voice.Name
              , voice => voice.VoiceId
              , StringComparer.OrdinalIgnoreCase);

            return parsed.Voices
                   .Select(voice => new VoiceInfo(voice.Name, "en", "US"))
                   .OrderBy(voice => voice.Name)
                   .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            return await _fallback.GetVoicesAsync();
        }
    }

    private string ResolveVoiceId(string? preferredName)
    {
        if (string.IsNullOrEmpty(preferredName))
            return DefaultVoiceId;

        if (_nameToVoiceId.TryGetValue(preferredName, out var voiceId))
            return voiceId;

        // Name not in cache — treat it as a raw voice_id (user-entered or persisted from a prior session)
        return preferredName;
    }

    private void DisposeCurrentPlayer()
    {
        try
        {
            _currentPlayer?.Stop();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
        }
        finally
        {
            _currentPlayer?.Dispose();
            _currentPlayer = null;
            _audioStream?.Dispose();
            _audioStream = null;
        }
    }

    // ── JSON DTOs (private — not part of the public API) ─────────────────────

    private sealed class ElevenLabsVoicesResponse
    {
        [JsonPropertyName("voices")]
        public IReadOnlyList<ElevenLabsVoiceDto> Voices { get; set; } = Array.Empty<ElevenLabsVoiceDto>();
    }

    private sealed class ElevenLabsVoiceDto
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
