using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.Knowledge.Journals.Clients;

public interface IJournalApiClient
{
    Task<JournalEntryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
