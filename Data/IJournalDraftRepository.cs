using LocalAIAssistant.PersonaAndContextEngine.Models;

namespace LocalAIAssistant.Data;

public interface IJournalDraftRepository
{
    Task                              AddAsync(JournalDraft draft);
    Task<IReadOnlyList<JournalDraft>> ListAsync();
    Task                              DeleteAsync(Guid id);
}