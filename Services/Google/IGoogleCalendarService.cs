namespace LocalAIAssistant.Services.Google;

public interface IGoogleCalendarService
{
    bool   HasToken  { get; }
    string ClientId  { get; }
    Task<bool> ConnectAsync();
    Task       DisconnectAsync();
    Task<string?> GetAccessTokenAsync();
}
