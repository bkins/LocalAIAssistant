using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Journal;

public interface IJournalApiClient
{
    Task<JournalEntryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
