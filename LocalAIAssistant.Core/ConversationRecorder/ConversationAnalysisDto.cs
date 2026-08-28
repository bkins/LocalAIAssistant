namespace LocalAIAssistant.Core.ConversationRecorder;

public class ConversationAnalysisDto
{
    public Guid                     Id                  { get; set; } = Guid.NewGuid();
    public Guid                     ConversationId      { get; set; }
    public string                   Summary             { get; set; } = string.Empty;
    public List<AnalysisDerivedItemDto> Topics              { get; set; } = new();
    public List<AnalysisDerivedItemDto> Questions           { get; set; } = new();
    public List<AnalysisDerivedItemDto> Decisions           { get; set; } = new();
    public List<AnalysisDerivedItemDto> ActionItems         { get; set; } = new();
    public List<AnalysisDerivedItemDto> ImportantStatements { get; set; } = new();
    public string                   Status              { get; set; } = "NotAnalyzed";
    public DateTime                 CreatedAtUtc        { get; set; } = DateTime.UtcNow;
    public DateTime?                AnalyzedAtUtc       { get; set; }
    public string?                  ModelUsed           { get; set; }
    public string?                  ErrorMessage        { get; set; }
    public bool                     IsDeleted           { get; set; }
    public DateTime?                DeletedUtc          { get; set; }
}
