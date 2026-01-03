using LocalAIAssistant.PersonaAndContextEngine.Models;

namespace LocalAIAssistant.PersonaAndContextEngine.Interfaces;

/// <summary>
/// This provides a seam for intent detection. Right now, it can be trivial (keywords → intent).
/// Later, replace it with an LLM-based classifier.
/// </summary>
public interface IIntentAnalyzer
{
    Task<IntentAnalysisResult> AnalyzeAsync(string input);
}