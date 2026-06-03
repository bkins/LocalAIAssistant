using LocalAIAssistant.Core.Coco.Models;

namespace LocalAIAssistant.Core.Coco;

public interface ICocoApiClient
{
    /// <summary>POST /rag/ask — non-streaming ask. Returns null when unreachable.</summary>
    Task<string?> AskAsync(string question, CancellationToken ct = default);

    /// <summary>POST /rag/ask-stream — SSE streaming ask. Yields AskEvent per SSE frame.</summary>
    IAsyncEnumerable<CocoAskEvent> AskStreamAsync(string question, CancellationToken ct = default);

    /// <summary>POST /rag/index — fire-and-forget indexing (server returns empty).</summary>
    Task IndexPathAsync(string path, bool force = false, CancellationToken ct = default);

    /// <summary>POST /rag/index-stream — SSE streaming index with progress events.</summary>
    IAsyncEnumerable<CocoIndexEvent> IndexStreamAsync(string path, bool force = false, CancellationToken ct = default);

    /// <summary>GET /rag/health — health status of the Coco API.</summary>
    Task<CocoHealthData?> GetHealthAsync(CancellationToken ct = default);

    /// <summary>GET /rag/stats — vector store statistics.</summary>
    Task<CocoStatsData?> GetStatsAsync(CancellationToken ct = default);

    /// <summary>GET /rag/status — combined health + stats in one call.</summary>
    Task<CocoStatusResult> GetStatusAsync(CancellationToken ct = default);

    /// <summary>GET /rag/models — list of available Ollama models.</summary>
    Task<IReadOnlyList<string>?> GetModelsAsync(CancellationToken ct = default);

    // ── Phase 4 stubs — endpoints planned but not yet in Coco API ────────────

    /// <summary>POST /rag/refactor — suggest a refactoring for a code snippet. COC-013 pending.</summary>
    Task<string?> RefactorAsync(string codeSnippet, CancellationToken ct = default);

    /// <summary>POST /rag/commit-message — generate a commit message from a diff. COC-013 pending.</summary>
    Task<string?> GenerateCommitMessageAsync(string diff, CancellationToken ct = default);

    /// <summary>POST /rag/explain — explain a code snippet in depth. COC-013 pending.</summary>
    Task<string?> ExplainAsync(string codeSnippet, CancellationToken ct = default);

    /// <summary>GET /rag/symbols — list indexed symbols/types from the vector store. COC-013 pending.</summary>
    Task<IReadOnlyList<string>?> GetIndexedSymbolsAsync(CancellationToken ct = default);
}
