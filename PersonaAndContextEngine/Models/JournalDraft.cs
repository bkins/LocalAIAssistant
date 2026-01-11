namespace LocalAIAssistant.PersonaAndContextEngine.Models;

public sealed class JournalDraft
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    // Raw canonical content
    public string Text { get; init; } = string.Empty;

    // Explicit syntax only (from J-02)
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string?               Mood { get; init; }

    // State
    public JournalDraftState State { get; init; } = JournalDraftState.Local;
}

public enum JournalDraftState
{
    Local  // created locally, not yet sent
  , Queued // offline intent (J-08)
  , Synced // acknowledged by API (optional future)
}
