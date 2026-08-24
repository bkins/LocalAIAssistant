namespace LocalAIAssistant.Core.ConversationRecorder;

public class ConversationRecordDto
{
    public Guid           Id            { get; set; }
    public string         Title         { get; set; } = string.Empty;
    public string         AudioFilePath { get; set; } = string.Empty;
    public string         MimeType      { get; set; } = "audio/wav";
    public TimeSpan       Duration      { get; set; }
    public long           FileSizeBytes { get; set; }
    public string         Status        { get; set; } = "NotProcessed";
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
