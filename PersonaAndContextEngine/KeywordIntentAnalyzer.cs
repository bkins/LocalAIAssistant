using LocalAIAssistant.PersonaAndContextEngine.Enums;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.PersonaAndContextEngine.Models;

namespace LocalAIAssistant.PersonaAndContextEngine;

public class KeywordIntentAnalyzer : IIntentAnalyzer
{
    private readonly Dictionary<string, (Intent intent, Guid? personaId)> _rules;

    public KeywordIntentAnalyzer(Dictionary<string, (Intent, Guid?)> rules)
    {
        _rules = rules;
    }

    public Task<IntentAnalysisResult> AnalyzeAsync(string userInput)
    {
        foreach (var (keyword, (intent, personaId)) in _rules)
        {
            if (userInput.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new IntentAnalysisResult
                                       {
                                           Intent             = intent
                                         , Confidence         = 0.8
                                         , SuggestedPersonaId = personaId
                                       });
            }
        }

        return Task.FromResult(new IntentAnalysisResult
                               {
                                   Intent     = Intent.GeneralHelp
                                 , Confidence = 0.5
                               });
    }
}
