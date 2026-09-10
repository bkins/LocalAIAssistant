namespace LocalAIAssistant.CognitivePlatform.DTOs;

/// <summary>Display-safe fact shown in the per-response "Why" affordance.</summary>
public sealed class TransparencyItemDto
{
    public string Label  { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
