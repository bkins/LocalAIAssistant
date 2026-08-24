namespace LocalAIAssistant.Core.ConversationRecorder;

public class TranscriptDto
{
    public Guid                    Id             { get; set; }
    public Guid                    ConversationId { get; set; }
    public string                  Status         { get; set; } = "NotProcessed";
    public List<TranscriptSegmentDto> Segments       { get; set; } = new();
    public bool                    IsDiarized     { get; set; }
    public DateTime                CreatedAtUtc   { get; set; }
    public DateTime?               ProcessedAtUtc { get; set; }
    public string?                 ErrorMessage   { get; set; }
}
