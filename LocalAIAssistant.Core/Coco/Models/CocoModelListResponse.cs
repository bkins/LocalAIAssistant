using System.Text.Json.Serialization;

namespace LocalAIAssistant.Core.Coco.Models;

public class CocoModelListResponse
{
    [JsonPropertyName("models")] public IReadOnlyList<CocoModelInfo>? Models { get; init; }
}

public class CocoModelInfo
{
    [JsonPropertyName("name")]  public string Name  { get; init; } = string.Empty;
    [JsonPropertyName("model")] public string Model { get; init; } = string.Empty;
}
