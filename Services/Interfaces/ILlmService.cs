using System.Runtime.CompilerServices;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Requests;

namespace LocalAIAssistant.Services.Interfaces;

public interface ILlmService
{
    // Backwards-compatible simpler overloads (kept)
    IAsyncEnumerable<string> SendPromptStreamingAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    Task<bool>               CheckApiHealthAsync();
    Task<string>             SendPromptStreamingAsync(Personality personality, string message);

    // New (as of 9/8/25) structured method (working towards this)
    IAsyncEnumerable<string>        SendPromptStreamingAsync(LlmRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    
}