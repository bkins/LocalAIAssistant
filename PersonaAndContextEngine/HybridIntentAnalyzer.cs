using System.Text;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.PersonaAndContextEngine.Enums;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.PersonaAndContextEngine.Models;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.PersonaAndContextEngine;

public class HybridIntentAnalyzer : IIntentAnalyzer
{
    private readonly ILlmService     _llm;
    private readonly IIntentAnalyzer _ruleBasedAnalyzer;
    private readonly ILoggingService _logger;
    
    private const double ConfidenceThreshold = 0.7;

    public HybridIntentAnalyzer(ILlmService     llm
                              , IIntentAnalyzer ruleBasedAnalyzer
                              , ILoggingService logger)
    {
        _llm               = llm;
        _ruleBasedAnalyzer = ruleBasedAnalyzer;
        _logger            = logger;
    }

    public async Task<IntentAnalysisResult> AnalyzeAsync(string input)
    {
        _logger.LogInformation($"HybridIntentAnalyzer.AnalyzeAsync: was invoked, with `input` of {input}."
                             , Category.HybridIntentAnalyzer);

        if (input.HasNoValue())
            return new IntentAnalysisResult { Intent = Intent.Unknown, Confidence = 0.0 };

        // --- Step 1: LLM classification ---
        var prompt = $"Classify this input into one of [{string.Join(", ", Enum.GetNames(typeof(Intent)))}]: {input}";
        var sb     = new StringBuilder();
        
        await foreach (var chunk in _llm.SendPromptStreamingAsync(prompt))
        {
            sb.Append(chunk);
        }
        
        _logger.LogInformation("HybridIntentAnalyzer.AnalyzeAsync: chunks read."
                             , Category.HybridIntentAnalyzer);

        var llmOutput   = sb.ToString();
        var cleanOutput = llmOutput.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        
        var parsed = Enum.TryParse<Intent>(cleanOutput, ignoreCase: true, out var intent);

        if (parsed.Not()) intent = Intent.Unknown;
        
        _logger.LogInformation($"HybridIntentAnalyzer.AnalyzeAsync: Intent was defined as {intent.GetDescription()}."
                             , Category.HybridIntentAnalyzer);

        var llmResult = new IntentAnalysisResult
                        {
                            Intent               = intent
                          , Confidence           = parsed ? 0.9 : 0.0
                          , SuggestedPersonaName = intent.GetDescription()
                        };

        // --- Step 2: Fallback to rule-based if LLM is unsure ---
        if (llmResult.Confidence >= ConfidenceThreshold)
            return llmResult;
        
        var fallbackResult = await _ruleBasedAnalyzer.AnalyzeAsync(input);
        fallbackResult.Metadata["FallbackUsed"] = "true";
        
        return fallbackResult;
    }
}
