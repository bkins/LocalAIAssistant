namespace LocalAIAssistant.Services.AiMemory;

public class MemoryRetrievalOptions
{
    public int      MaxStmMessages       { get; set; } = 6;    // recent turns
    public int      MaxLtmSnippets       { get; set; } = 6;    // matching facts
    public int      SummaryMaxChars      { get; set; } = 1200; // prompt budget
    public bool     SummarizeOnPromotion { get; set; } = true;
    public TimeSpan LtmRecencyWindow     { get; set; } = TimeSpan.FromDays(90);
    public bool     IncludeTimestamps    { get; set; } = false;
    public int      PromotionBatchSize   { get; set; } = 8;  // how many oldest to summarize

}