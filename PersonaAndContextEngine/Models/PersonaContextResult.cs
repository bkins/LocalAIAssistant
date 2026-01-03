using LocalAIAssistant.Data.Models;
using LocalAIAssistant.PersonaAndContextEngine.Enums;
using LocalAIAssistant.Services.AiMemory;

namespace LocalAIAssistant.PersonaAndContextEngine.Models;

public class PersonaContextResult
{

    public Personality          Personality              { get; set; } = default!;
    public Intent               Intent               { get; set; } = default!;
    public IntentAnalysisResult IntentAnalysisResult { get; set; } = new();
    public MemoryContext        Memory               { get; set; } = new();

}
