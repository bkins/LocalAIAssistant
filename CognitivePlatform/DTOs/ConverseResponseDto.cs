namespace LocalAIAssistant.CognitivePlatform.DTOs;

public class ConverseResponseDto
{
    public string                    Message        { get; set; } = "";
    public string                    ConversationId { get; set; } = "default";
    public bool                      WasFastPath    { get; set; }
    public IReadOnlyList<InsightDto> Insights       { get; set; } = Array.Empty<InsightDto>();
    // Populated when the API falls back to a lower model tier.
    // If null the client attempts to extract the notice from trailing italic text in Message.
    public string?                   ModelNotice    { get; set; }
}