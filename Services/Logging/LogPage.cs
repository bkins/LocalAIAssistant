namespace LocalAIAssistant.Services.Logging;

public sealed record LogPage( IReadOnlyList<LogEntry> Entries
                            , int                     Offset
                            , bool                    HasMore
                            , int                     MalformedLineCount);
