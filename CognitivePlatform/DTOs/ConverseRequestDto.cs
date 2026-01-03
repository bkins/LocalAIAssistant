namespace LocalAIAssistant.CognitivePlatform.DTOs;

public class ConverseRequestDto
{
    public string  SessionId { get; set; } = string.Empty;
    public string? Input     { get; set; }
    public string? Model     { get; set; }
}