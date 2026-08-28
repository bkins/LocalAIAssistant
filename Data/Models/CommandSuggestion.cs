namespace LocalAIAssistant.Data.Models;

public record CommandSuggestion
{
    public string Prefix          { get; init; } = string.Empty;
    public string Title           { get; init; } = string.Empty;
    public string Description     { get; init; } = string.Empty;
    public string CommandTemplate { get; init; } = string.Empty;
    public string Icon            { get; init; } = "⚡";
}
