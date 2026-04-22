namespace LocalAIAssistant.Knowledge.Journals.Models;

public sealed class JournalEntryDto
{
    public  Guid                  Id   { get; init; }
    public  string                Text { get; init; } = string.Empty;
    private DateTimeOffset        _createAt;

    public DateTimeOffset CreatedAt
    {
        get
        {
            return _createAt.LocalDateTime;
        }
        set
        {
            _createAt = value;
        }
    }
    public DateTimeOffset        CreatedAtLocalTime { get; init; }
    public IReadOnlyList<string> Tags               { get; init; } = Array.Empty<string>();
    
    public string?           Mood      { get; init; }
    public JournalEntryState State     { get; init; }
    public int?              MoodScore { get; set; }
    public bool              IsEdited  { get; init; }
}

public enum JournalEntryState
{
    Local
  , Draft
  , Queue
  , Committed
  , Active
  , Synced
  , Edited
}