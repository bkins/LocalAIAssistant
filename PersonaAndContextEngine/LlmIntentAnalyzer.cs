using System.Text;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.PersonaAndContextEngine.Enums;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.PersonaAndContextEngine.Models;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.PersonaAndContextEngine;

public class LlmIntentAnalyzer : IIntentAnalyzer
{
    private readonly ILlmService _llm;

    public LlmIntentAnalyzer(ILlmService llm)
    {
        _llm = llm;
    }

    public async Task<IntentAnalysisResult> AnalyzeAsync(string input)
    {
        var prompt = $@"You are an intent classifier. Classify this input into one of [{string.Join(", ", Enum.GetNames(typeof(Intent)))}]: {input}
Respond with only the label, nothing else.";

        // Collect all chunks into a single string
        var sb = new StringBuilder();
        await foreach (var chunk in _llm.SendPromptStreamingAsync(prompt))
        {
            sb.Append(chunk);
        }

        var rawResult = sb.ToString().Trim();
        // Try to parse the output into your enum
        if (Enum.TryParse<Intent>(rawResult
                                , ignoreCase: true
                                , out var intent).Not())
        {
            intent = Intent.Unknown;
        }

        return new IntentAnalysisResult
               {
                   Intent               = intent
                 , Confidence           = 0.8
                 , SuggestedPersonaName = intent.GetDescription()
               };
    }
}
