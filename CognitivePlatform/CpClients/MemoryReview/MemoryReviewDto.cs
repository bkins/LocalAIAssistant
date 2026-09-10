namespace LocalAIAssistant.CognitivePlatform.CpClients.MemoryReview;

public sealed class MemoryReviewDto
{
    public IReadOnlyList<MemoryReviewItemDto>   Items   { get; init; } = [];
    public IReadOnlyList<MemoryReviewSourceDto> Sources { get; init; } = [];
}

public sealed class MemoryReviewItemDto
{
    public string   SourceKind                    { get; init; } = string.Empty;
    public string   SourceId                      { get; init; } = string.Empty;
    public string   SourceLabel                   { get; init; } = string.Empty;
    public string   State                         { get; init; } = string.Empty;
    public string   Content                       { get; init; } = string.Empty;
    public double?  Confidence                      { get; init; }
    public DateTimeOffset LastReinforcedOrModifiedUtc { get; init; }
    public IReadOnlyList<string> Provenance         { get; init; } = [];
    public IReadOnlyList<string> AvailableOperations { get; init; } = [];
}

public sealed class MemoryReviewSourceDto
{
    public string  SourceKind  { get; init; } = string.Empty;
    public bool    IsAvailable { get; init; }
    public string? Message     { get; init; }
}
