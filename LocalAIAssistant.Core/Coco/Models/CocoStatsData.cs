using System.Text.Json.Serialization;

namespace LocalAIAssistant.Core.Coco.Models;

public class CocoStatsResponse
{
    [JsonPropertyName("success")]   public bool          Success   { get; init; }
    [JsonPropertyName("error")]     public string?       Error     { get; init; }
    [JsonPropertyName("timestamp")] public string?       Timestamp { get; init; }
    [JsonPropertyName("data")]      public CocoStatsData? Data     { get; init; }
}

public class CocoStatsData
{
    [JsonPropertyName("totalChunks")] public int       TotalChunks { get; init; }
    [JsonPropertyName("uniqueFiles")] public int       UniqueFiles { get; init; }
    [JsonPropertyName("lastUpdated")] public DateTime? LastUpdated { get; init; }
}
