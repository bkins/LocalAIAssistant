using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Journal;

public interface IJournalApiClient
{
    Task<JournalEntryDto?>                   GetByIdAsync(Guid      id,        CancellationToken ct = default);
    Task<IReadOnlyList<JournalRevisionDto>?> GetRevisionsAsync(Guid journalId, CancellationToken ct = default);

    Task EditEntryAsync (Guid                   journalId
                      , string                 text
                      , IReadOnlyList<string>? parseTags
                      , string?                mood
                      , int?                   moodScore);
}
