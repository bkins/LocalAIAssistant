namespace LocalAIAssistant.CognitivePlatform.DTOs;

public class InsightDto
{
    public string  Message         { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
    public string  Category        { get; set; } = "General";
    public string  Priority        { get; set; } = "Normal";
}
