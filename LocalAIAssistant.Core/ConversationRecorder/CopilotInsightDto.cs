namespace LocalAIAssistant.Core.ConversationRecorder;

public class CopilotInsightDto
{
    public Guid     Id                 { get; set; } = Guid.NewGuid();
    public Guid     ConversationId     { get; set; }
    public DateTime TimestampUtc       { get; set; } = DateTime.UtcNow;
    public double   AudioOffsetSeconds { get; set; }
    public string   InsightType        { get; set; } = "RecallHint";
    public string   Headline           { get; set; } = string.Empty;
    public string   Detail             { get; set; } = string.Empty;
    public float    RelevanceScore     { get; set; } = 1.0f;
    public string   ProvenanceChain    { get; set; } = string.Empty;
    public bool     IsDismissed        { get; set; }
}

public class CopilotSliceResultDto
{
    public Guid                   ConversationId       { get; set; }
    public int                    SliceIndex           { get; set; }
    public string                 TranscribedText      { get; set; } = string.Empty;
    public List<CopilotInsightDto> Insights            { get; set; } = new();
    public bool                   HasActionableInsight => Insights != null && Insights.Count > 0;
    public DateTime               ProcessedAtUtc       { get; set; } = DateTime.UtcNow;
}
