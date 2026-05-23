namespace LocalAIAssistant.Core.ConversationHistory;

public class ConversationTurnDto
{
    public string         Role      { get; init; } = string.Empty;
    public string         Content   { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
}
