using LocalAIAssistant.Data.Models;
using LocalAIAssistant.PersonaAndContextEngine.Enums;

namespace LocalAIAssistant.PersonaAndContextEngine.Models;

public class ContextResult
{
    public Personality Personality { get; set; } = new Personality();
    public Intent  Intent  { get; set; } = new Intent();
}
