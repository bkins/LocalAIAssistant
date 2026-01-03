using LocalAIAssistant.Data.Models;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.PersonaAndContextEngine.Models;

namespace LocalAIAssistant.Data;

public class InMemoryPersonaRepository : IPersonaRepository
{
    private readonly Dictionary<string, Personality> _personas = new(StringComparer.OrdinalIgnoreCase);

    public Task<Personality?> GetByIdAsync(Guid id) => throw new NotImplementedException();

    public Task<Personality?> GetDefaultPersonaAsync() => throw new NotImplementedException();

    public Task<IEnumerable<Personality>> GetAllAsync()
    {
        return Task.FromResult(_personas.Values.AsEnumerable());
    }

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();

    public Task<Personality?> GetByNameAsync(string name)
    {
        _personas.TryGetValue(name, out var persona);
        return Task.FromResult(persona);
    }

    public Task AddOrUpdateAsync(Personality persona)
    {
        _personas[persona.Name] = persona;
        return Task.CompletedTask;
    }
}
