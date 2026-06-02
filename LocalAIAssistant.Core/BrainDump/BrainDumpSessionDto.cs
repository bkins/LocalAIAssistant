namespace LocalAIAssistant.Core.BrainDump;

public class BrainDumpSessionDto
{
    public string          Id               { get; set; } = string.Empty;
    public DateTimeOffset  CreatedAt        { get; set; }
    public DateTimeOffset  UpdatedAt        { get; set; }
    public bool            Processed        { get; set; }
    public DateTimeOffset? ProcessedAt      { get; set; }
    public string?         Avoidance        { get; set; }
    public string?         Fears            { get; set; }
    public string?         Frustrations     { get; set; }
    public string?         Discouragements  { get; set; }
    public string?         GoalsAndBarriers { get; set; }
    public string?         HurtAndSorrow    { get; set; }
    public string?         SelfCriticism    { get; set; }
    public string?         ExtractionSummary   { get; set; }
    public List<string>    ExtractedTaskIds    { get; set; } = new();
    public List<string>    ExtractedInsightIds { get; set; } = new();
}
