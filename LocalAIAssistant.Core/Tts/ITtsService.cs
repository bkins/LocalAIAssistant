namespace LocalAIAssistant.Core.Tts;

public interface ITtsService
{
    Task                         SpeakAsync(string text, CancellationToken ct = default);
    Task                         StopAsync();
    Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync();

    string? PreferredVoiceName { get; set; }
    bool    IsEnabled          { get; set; }
    bool    IsTtsAvailable     { get; }
}
