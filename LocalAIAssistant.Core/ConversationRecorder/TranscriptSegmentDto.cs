namespace LocalAIAssistant.Core.ConversationRecorder;

public class TranscriptSegmentDto
{
    public Guid     Id        { get; set; }
    public TimeSpan Start     { get; set; }
    public TimeSpan End       { get; set; }
    public string   Text      { get; set; } = string.Empty;
    public string?  SpeakerId { get; set; }
}
