namespace LocalAIAssistant.Presentation;

/// <summary>
/// A typed, UI-neutral description of read-only content that a built-in host can render.
/// </summary>
public sealed record PresentationModel(
    PresentationStyle Style,
    string Title,
    string? Summary,
    IReadOnlyList<PresentationBadge> Badges,
    DateTimeOffset? LastModifiedAt);

public sealed record PresentationBadge(string Label, string Value);

public enum PresentationStyle
{
    Card
}
