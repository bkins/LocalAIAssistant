using LocalAIAssistant.Extensions;
using LocalAIAssistant.PersonaAndContextEngine.Enums;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.PersonaAndContextEngine.Models;

namespace LocalAIAssistant.PersonaAndContextEngine;

/// <summary>
/// Rule-based implementation of IIntentAnalyzer.
/// Uses simple keyword matching to assign intents and suggest personas.
/// Later, this can be replaced with an LLM or ML-based classifier.
/// </summary>
public class RuleBasedIntentAnalyzer : IIntentAnalyzer
{
    public Task<IntentAnalysisResult> AnalyzeAsync(string input)
    {
        if (input.HasNoValue())
        {
            return Task.FromResult(new IntentAnalysisResult
                                   {
                                       Intent               = Intent.Unknown
                                     , Confidence           = 0.0
                                     , SuggestedPersonaId   = null
                                     , SuggestedPersonaName = null!
                                   });
        }

        input = input.ToLowerInvariant();
        var result = new IntentAnalysisResult();

        // --- Rule 1: Technical Help ---
        if (input.Contains("code") 
         || input.Contains("bug") 
         || input.Contains("compile") 
         || input.Contains("error"))
        {
            result.Intent               = Intent.TechnicalHelp;
            result.Confidence           = 0.9;
            result.SuggestedPersonaName = "TechnicalHelper";
            
            return Task.FromResult(result);
        }

        // --- Rule 2: Leadership / Team Guidance ---
        if (input.Contains("team") 
         || input.Contains("lead") 
         || input.Contains("manage") 
         || input.Contains("conflict"))
        {
            result.Intent               = Intent.Leadership;
            result.Confidence           = 0.85;
            result.SuggestedPersonaName = "LeadershipCoach";
            return Task.FromResult(result);
        }

        // --- Rule 3: Motivation / Inspiration ---
        if (input.Contains("motivate") 
         || input.Contains("inspire") 
         || input.Contains("encourage") 
         || input.Contains("burnout"))
        {
            result.Intent               = Intent.Motivation;
            result.Confidence           = 0.8;
            result.SuggestedPersonaName = "Motivator";
            return Task.FromResult(result);
        }

        // --- Rule 4: General Help ---
        if (input.Contains("how do i") 
         || input.Contains("what is") 
         || input.Contains("explain"))
        {
            result.Intent               = Intent.GeneralHelp;
            result.Confidence           = 0.7;
            result.SuggestedPersonaName = "GeneralHelper";
            return Task.FromResult(result);
        }

        // --- Fallback ---
        result.Intent               = Intent.Unknown;
        result.Confidence           = 0.1;
        result.SuggestedPersonaName = null;

        return Task.FromResult(result);
    }
}
