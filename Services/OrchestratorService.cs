using System.Runtime.CompilerServices;
using System.Text;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.PersonaAndContextEngine.Interfaces;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Contracts;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.Services;

public class OrchestratorService : IOrchestratorService
{
    private readonly ILlmService                      _llm;
    private readonly IPersonalityService              _personalityService;
    private readonly OllamaConfigService              _configService;
    private readonly IConversationMemory?             _conversationMemory; // optional, if you want STM logging here
    private readonly IMemoryService                   _memoryService;      // for LTM saves (already used by LlmService too)
    private readonly ILoggingService                  _log;
    private readonly IPersonaAndContextEngine         _contextEngine;
    private readonly IOptions<MemoryRetrievalOptions> _memoryRetrievalOptions;

    public OrchestratorService(ILlmService                      llm
                             , IPersonalityService              personalityService
                             , OllamaConfigService              configService
                             , IMemoryService                   memoryService
                             , ILoggingService                  log
                             , IPersonaAndContextEngine         contextEngine
                             , IOptions<MemoryRetrievalOptions> memoryRetrievalOptions
                             , IConversationMemory?             conversationMemory = null)
    {
        _llm                    = llm;
        _personalityService     = personalityService;
        _configService          = configService;
        _memoryService          = memoryService;
        _log                    = log;
        _contextEngine          = contextEngine;
        _conversationMemory     = conversationMemory; // can be null
        _memoryRetrievalOptions = memoryRetrievalOptions;
    }
    
    /// <summary>
    /// Entry point for VM: wrap ProcessAsync for simplicity.
    /// </summary>
    public IAsyncEnumerable<string> SendPromptAsync(string                                     userMessage
                                                  , Personality                                personality
                                                  , [EnumeratorCancellation] CancellationToken ct = default)
    {
        // simple wrapper to keep the interface you asked for
        return ProcessAsync(userMessage
                          , personality
                          , ct);
    }


    /// <summary>
    /// Main pipeline: fuse user input, memory, personality, system prompt,
    /// and config, then stream response from LLM.
    /// </summary>
    public async IAsyncEnumerable<string> ProcessAsync(string                                     userInput
                                                     , Personality                                personality
                                                     , [EnumeratorCancellation] CancellationToken ct = default)
    {
        var turnId = Guid.NewGuid().ToString("N");
        if (personality == null) throw new ArgumentNullException(nameof(personality));

        // Ensure personality has a config - don't mutate the caller's personality if possible
        var effectiveConfig = personality.OllamConfiguration ?? _configService.GetConfig();

        LogEvent(turnId
               , $"STARTED using '{personality.Name}' and Model '{effectiveConfig?.Model}'");

        // Optionally set Current (safe but not strictly required if we pass personality around)
        _personalityService.SetCurrent(personality);

        // Record the user message in STM (fire-and-forget is OK but await when possible)
        RecordUserMessage(userInput
                        , turnId);

        // 1) Retrieve memory/context for this turn (correct API)
        var memCtx = await _memoryService.GetContextForTurnAsync(userInput
                                                               , _memoryRetrievalOptions
                                                               , ct)
                                         .ConfigureAwait(false);
        var contextSummary = memCtx?.Summary ?? string.Empty;

        // 2) Build the LlmRequest that LlmService expects
        var req = new LlmRequest
                  {
                          UserPrompt   = userInput
                        , SystemPrompt = personality.SystemPrompt ?? "You are a helpful AI."
                        , Context      = contextSummary
                        , Personality  = personality
                        , OllamaConfig = effectiveConfig
                  };

        // 3) Stream via ILlmService (structured request overload)
        var sb = new StringBuilder();
        await foreach (var chunk in _llm.SendPromptStreamingAsync(req
                                                                , ct).WithCancellation(ct).ConfigureAwait(false))
        {
            if (chunk.HasNoValue()) continue;

            sb.Append(chunk);
            yield return chunk; // stream chunk back to caller (ChatViewModel)
        }

        var assistantText = sb.ToString();

        // 4) Persist assistant result (STM + LTM)
        await PersistAssistantMessage(assistantText
                                    , turnId).ConfigureAwait(false);

        LogEvent(turnId
               , $"Completed tokens_out≈{assistantText.Length}");
    }

    public async IAsyncEnumerable<string> HandleUserMessageStreamingAsync(string                                     userInput
                                                                        , Guid?                                      forcedPersonaId = null
                                                                        , [EnumeratorCancellation] CancellationToken ct              = default)
    {
        var contextResult = await _contextEngine.ResolveContextAsync(userInput, forcedPersonaId, ct)
                                                .ConfigureAwait(false);

        LogEvent(Guid.NewGuid().ToString("N")
               , $"Streaming START: Persona={contextResult.Personality.Name}, Intent={contextResult.IntentAnalysisResult.Intent}");

        await foreach (var chunk in ProcessAsync(userInput, contextResult.Personality, ct))
            yield return chunk;
    }

    private void ApplyPersonalityConfiguration(Personality?              personality
                                             , string                    turnId
                                             , [CallerMemberName] string caller = null!)
    {
        if (personality?.OllamConfiguration is null)
        {
            LogEvent(caller, $"ERROR: Personality configuration is null.  Cannot Apply a configuration if the Personality is null.");
            return;
        }

        var ollamaConfig = personality.OllamConfiguration;
        
        // defaults
        if (ollamaConfig.Model.HasNoValue())
            ollamaConfig.Model = "qwen2.5:14b";
        
        if (ollamaConfig.Host.HasNoValue())
            ollamaConfig.Host = StringConsts.OllamaServerUrl;
        
        try
        {
            _configService.UpdateConfig(new OllamaConfig
                                        {
                                                Model       = ollamaConfig.Model
                                              , Host        = ollamaConfig.Host
                                              , NumPredict  = ollamaConfig.NumPredict
                                              , Temperature = ollamaConfig.Temperature
                                        });
        }
        catch (Exception e)
        {
            _log.LogError(e, "Error from: {Source}",  Category.Orchestrator);
            // _log.LogError(e
            //             , "Error from: {Source}"
            //             , caller);
        }

        LogEvent(turnId
               , $"Applied OllamaConfig: model={ollamaConfig.Model}, host={ollamaConfig.Host}");
    }

    private async Task PersistAssistantMessage(string assistantText
                                             , string turnId)
    {
        try
        {
            // STM
            _conversationMemory?.AddAsync(new Message
                                          {
                                                  Sender    = Senders.Assistant
                                                , Content   = assistantText
                                                , Timestamp = DateTime.UtcNow
                                          });

            // LTM (your MemoryService.SaveEntryAsync writes to LTM if configured)
            await _memoryService.SaveEntryAsync(Senders.Assistant, assistantText, DateTime.UtcNow);
            LogEvent(turnId, $"Assistant's text saved to memory: {Truncate(assistantText, 25)}...");
        }
        catch (Exception ex)
        {
            _log.LogError(ex
                        , $"[Orchestrator] turn={turnId} failed to save assistant response to memory stores"
                        , Category.Orchestrator);
        }
    }

    private void RecordUserMessage(string userInput
                                 , string turnId)
    {
        try
        {
            _conversationMemory?.AddAsync(new Message
                                          {
                                                  Sender    = Senders.User
                                                , Content   = userInput
                                                , Timestamp = DateTime.UtcNow
                                          });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"[Orchestrator] turn={turnId} failed to add user message to STM");
        }
    }

    private async Task<string> RunPipelineAsync(Personality personality, string userInput, CancellationToken ct)
    {
        var sb = new StringBuilder();
        await foreach (var chunk in ProcessAsync(userInput, personality, ct))
            sb.Append(chunk);
        return sb.ToString();
    }

    private Task<Personality?> LoadPersonalityAsync(Personality personality)
    {
        // TODO: wire into repository later
        return Task.FromResult<Personality?>(personality);
    }

    private static string Truncate(string value, int maxLength)
        => string.IsNullOrEmpty(value) ? value : value[..Math.Min(value.Length, maxLength)];
    
    private void LogEvent(string turnId, string logEvent)
    {
        _log.LogInformation($"Turn: {turnId} {logEvent}" , Category.Orchestrator);
    }
}