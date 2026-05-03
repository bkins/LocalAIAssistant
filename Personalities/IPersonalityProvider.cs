using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Personalities;

public interface IPersonalityProvider
{
    IEnumerable<Personality> Load();
}