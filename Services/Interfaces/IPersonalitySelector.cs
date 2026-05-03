using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.Interfaces;

public interface IPersonalitySelector
{
    Personality? Select(string input, IEnumerable<Personality> personalities);
}