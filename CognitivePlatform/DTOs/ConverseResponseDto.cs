namespace LocalAIAssistant.CognitivePlatform.DTOs;

public class ConverseResponseDto
{
    public string                    Message        { get; set; } = "";
    public string                    ConversationId { get; set; } = "default";
    public bool                      WasFastPath    { get; set; }
    public IReadOnlyList<InsightDto> Insights       { get; set; } = Array.Empty<InsightDto>();
}