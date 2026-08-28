namespace LocalAIAssistant.Core.ConversationRecorder;

public class AnalysisDerivedItemDto
{
    public Guid       Id                         { get; set; } = Guid.NewGuid();
    public Guid       ConversationId             { get; set; }
    public string     Type                       { get; set; } = string.Empty;
    public string     Content                    { get; set; } = string.Empty;
    public List<Guid> SourceTranscriptSegmentIds { get; set; } = new();
}
