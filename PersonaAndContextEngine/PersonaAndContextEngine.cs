using LocalAIAssistant.Data.Models;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.PersonaAndContextEngine.Models;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.PersonaAndContextEngine;

/*
 * Role of PersonaAndContextEngine
 * * Input → Analysis: Take the raw user query, along with conversation history and active personality.
 * * Context Enrichment: Apply memory, intent analysis, or signals from the orchestrator (e.g., last active persona, explicit persona override).
 * * Output → Decision: Return a PersonaContextResult that specifies:
 *     * Which persona to use (could be explicit or inferred).
 *     * Any intent tags (e.g., "technical", "leadership", "motivation").
 *     * The prepared prompt/context block for the orchestrator to pass into the LLM.
 */
    
public class PersonaAndContextEngine : IPersonaAndContextEngine
{
    private readonly IPersonaRepository               _personaRepo;
    private readonly IMemoryService                   _memoryService;
    private readonly IIntentAnalyzer                  _intentAnalyzer;
    private readonly ILoggingService                  _logger;
    private readonly IOptions<MemoryRetrievalOptions> _memOpts;

    private const string ClassName = nameof(PersonaAndContextEngine);

    public PersonaAndContextEngine(IPersonaRepository personaRepo
                                 , IMemoryService     memoryService
                                 , IIntentAnalyzer    intentAnalyzer
                                 , ILoggingService    logger
        , IOptions<MemoryRetrievalOptions>            memOpts)
    {
        _personaRepo    = personaRepo;
        _memoryService  = memoryService;
        _intentAnalyzer = intentAnalyzer;
        _logger         = logger;
        _memOpts        = memOpts;
    }

    public async Task<PersonaContextResult> ResolveContextAsync(string            userInput
                                                              , Guid?             forcedPersonaId = null
                                                              , CancellationToken ct              = default)
    {
        LogEvent($"Resolving persona and context for input: {userInput}");

        Personality? persona = null;

        // 1. Forced persona (manual override)
        if (forcedPersonaId.HasValue)
        {
            persona = await _personaRepo.GetByIdAsync(forcedPersonaId.Value);
            LogEvent($"Forced persona selected: {persona?.Name} ({forcedPersonaId})");
        }

        // 2. Intent analysis (suggests persona if available)
        var analysisResult = await _intentAnalyzer.AnalyzeAsync(userInput)
                                                  .ConfigureAwait(false);

        if (persona == null 
         && analysisResult.SuggestedPersonaId != null)
        {
            persona = await _personaRepo.GetByIdAsync(analysisResult.SuggestedPersonaId.Value);
            LogEvent($"Persona suggested by intent analyzer: {persona?.Name} ({analysisResult.SuggestedPersonaId})");
        }

        LogEvent($"Intent='{analysisResult.Intent}' Confidence={analysisResult.Confidence:0.00}");

        // 3. Default fallback
        if (persona == null)
        {
            persona = await _personaRepo.GetDefaultPersonaAsync();
            LogEvent($"Fallback to default persona: {persona?.Name}");
        }

        // 4. Enrich with memory/context
        var newMemoryOptions = new MemoryRetrievalOptions
                               {
                                   MaxStmMessages    = 5
                                 , MaxLtmSnippets    = 3
                                 , SummaryMaxChars   = 1000
                                 , LtmRecencyWindow  = TimeSpan.FromDays(30)
                                 , IncludeTimestamps = false
                               };
        _memOpts.Value.MaxStmMessages = newMemoryOptions.MaxStmMessages;
        _memOpts.Value.MaxLtmSnippets = newMemoryOptions.MaxLtmSnippets;
        _memOpts.Value.SummaryMaxChars = newMemoryOptions.SummaryMaxChars;
        _memOpts.Value.LtmRecencyWindow = newMemoryOptions.LtmRecencyWindow;
        _memOpts.Value.IncludeTimestamps = newMemoryOptions.IncludeTimestamps;
        
        var memoryContext = await _memoryService.GetContextForTurnAsync(userInput
                                                                      , _memOpts
                                                                      , ct);

        LogEvent($"Memory summary chars={memoryContext.Summary.Length}");

        return new PersonaContextResult
               {
                   Personality          = persona!
                 , IntentAnalysisResult = analysisResult
                 , Memory               = memoryContext
               };
    }

    private void LogEvent(string message)
    {
        _logger.LogInformation($"[{ClassName}] {message}");
    }
}
