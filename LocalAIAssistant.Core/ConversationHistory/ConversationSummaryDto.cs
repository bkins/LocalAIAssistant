namespace LocalAIAssistant.Core.ConversationHistory;

public class ConversationSummaryDto
{
    public string   ConversationId { get; set; } = "";
    public string?  Name           { get; set; }
    public DateTime LastActiveUtc  { get; set; }
    public int      MessageCount   { get; set; }
}
