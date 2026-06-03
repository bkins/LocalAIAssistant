namespace LocalAIAssistant.Core.Coco.Models;

/// <summary>
/// Combined view of Coco health + stats for the Settings page.
/// </summary>
public class CocoStatusResult
{
    public bool    IsReachable       { get; init; }
    public bool    OllamaAvailable   { get; init; }
    public bool    StorageAvailable  { get; init; }
    public bool    IndexingAvailable { get; init; }
    public int     TotalChunks       { get; init; }
    public int     UniqueFiles       { get; init; }
    public DateTime? LastIndexed     { get; init; }
    public string? ErrorMessage      { get; init; }

    public string Summary =>
        IsReachable
            ? $"Connected — {UniqueFiles} files, {TotalChunks} chunks"
              + (LastIndexed.HasValue
                    ? $", indexed {LastIndexed.Value:g}"
                    : string.Empty)
            : $"Unavailable{(ErrorMessage is not null ? $": {ErrorMessage}" : string.Empty)}";
}
