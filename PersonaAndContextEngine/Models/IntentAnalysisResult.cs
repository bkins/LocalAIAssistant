using LocalAIAssistant.PersonaAndContextEngine.Enums;

namespace LocalAIAssistant.PersonaAndContextEngine.Models;

/// <summary>
/// Intent analysis should tell us what kind of request this is and optionally suggest a persona switch.
/// </summary>
public class IntentAnalysisResult
{
    public Intent                     Intent               { get; set; } = Intent.GeneralHelp;
    public double                     Confidence           { get; set; } = 0.0;    // for future LLM-powered analyzers
    public Guid?                      SuggestedPersonaId   { get; set; }           // null = no switch suggested
    public Dictionary<string, string> Metadata             { get; set; } = new();  // extensibility
    public string                     SuggestedPersonaName { get; set; }

}
