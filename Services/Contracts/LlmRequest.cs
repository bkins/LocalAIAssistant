using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.Requests;
/*
 * The orchestrator will build this and pass it to ILlmService.
 * LlmService can then use OllamaConfig from the request (if present) or fall back to its configured default.
 */
public sealed class LlmRequest
{
    public string        UserPrompt   { get; init; } = string.Empty;
    public string        SystemPrompt { get; init; } = "You are a helpful AI.";
    public string?       Context      { get; init; } // memory summary or retrieval result
    public Personality?  Personality  { get; init; } // for logging / downstream use
    public OllamaConfig? OllamaConfig { get; init; } // optional per-request override
}