namespace LocalAIAssistant.Services.Logging;

public sealed record LogLineRecord( long   Offset
                                  , string Text)
{
    public string StorageId => LogEntryIdentity.Create(Offset, Text);
}
