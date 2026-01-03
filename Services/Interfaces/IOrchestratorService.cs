using System.Runtime.CompilerServices;
using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.Interfaces;

public interface IOrchestratorService
{
    /// <summary>
    /// Orchestrates a single turn. Streams assistant output as it arrives.
    /// </summary>
    IAsyncEnumerable<string> ProcessAsync(string userInput, Personality personality, CancellationToken ct = default);

    IAsyncEnumerable<string> HandleUserMessageStreamingAsync(string                                     userInput
                                                           , Guid?                                      forcedPersonaId = null
                                                           , [EnumeratorCancellation] CancellationToken ct              = default);

    IAsyncEnumerable<string> SendPromptAsync(string            userMessage
                                           , Personality       personality
                                           , CancellationToken cancellationToken = default);

}

