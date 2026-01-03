using LocalAIAssistant.Data.Models;
using LocalAIAssistant.PersonaAndContextEngine.Models;

namespace LocalAIAssistant.PersonaAndContextEngine.Interfaces;

/// <summary>
/// This abstracts how to store/retrieve personas.
/// Could be in-memory at first, later persisted (SQLite, JSON, etc.).
/// </summary>
public interface IPersonaRepository
{
    Task<Personality?>             GetByIdAsync(Guid id);
    Task<Personality?>             GetDefaultPersonaAsync();
    Task<IEnumerable<Personality>> GetAllAsync();
    Task                           AddOrUpdateAsync(Personality persona);
    Task                           DeleteAsync(Guid             id);
    Task<Personality>              GetByNameAsync(string        personaName);

}
