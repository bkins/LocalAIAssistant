namespace LocalAIAssistant.Core.Coco;

/// <summary>
/// Heuristic classifier that decides whether a user query should be routed to
/// Coco (code intelligence) instead of the default CognitivePlatform backend.
///
/// Rules:
///   - Explicit "use Coco" / "ask Coco" → always Coco.
///   - Explicit "ask CP" / "use CP" / "ask personal" → always CP (override).
///   - Two or more code-specific keyword hits → Coco.
///   - Otherwise → CP (default).
/// </summary>
public static class CodeIntentAnalyzer
{
    private static readonly string[] CodeKeywords =
    {
        "what does"  , "how does"      , "explain this"   , "explain the"
      , "what calls" , "where is"      , "find usages"    , "find usage"
      , "show me"    , "how is"        , "refactor"       , "implement"
      , "namespace"  , "interface "    , "using "         , ".cs"
      , "method"     , "property"      , "constructor"    , "async "
      , "await "     , "foreach"       , "linq"           , "delegate"
      , "event "     , "override"      , "virtual"        , "list<"
      , "ienumerable", "task<"         , "dictionary<"    , "csproj"
      , "class "     , "public "       , "private "       , "return "
      , "null check" , "null reference", "exception"      , "stack trace"
    };

    private static readonly string[] ExplicitCocoTerms =
    {
        "ask coco", "use coco", "coco:"
    };

    private static readonly string[] ExplicitCpTerms =
    {
        "ask cp", "use cp", "ask cognitive", "ask personal", "cp:"
    };

    // ── Phase 4 intent keywords — plumbing for endpoints that are COC-013 pending ──

    private static readonly string[] RefactorTerms =
    {
        "refactor this", "refactor the", "suggest refactoring", "how should i refactor"
    };

    private static readonly string[] CommitMessageTerms =
    {
        "commit message", "suggest a commit", "generate commit", "write a commit"
    };

    private static readonly string[] ExplainCodeTerms =
    {
        "explain this code", "explain the code", "explain this function", "explain this method"
      , "explain this class"
    };

    private static readonly string[] SymbolsTerms =
    {
        "what symbols", "what is indexed", "list symbols", "indexed symbols", "what types are indexed"
    };

    public static bool IsRefactorRequest(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var lower = input.ToLowerInvariant();
        return RefactorTerms.Any(term => lower.Contains(term));
    }

    public static bool IsCommitMessageRequest(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var lower = input.ToLowerInvariant();
        return CommitMessageTerms.Any(term => lower.Contains(term));
    }

    public static bool IsExplainCodeRequest(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var lower = input.ToLowerInvariant();
        return ExplainCodeTerms.Any(term => lower.Contains(term));
    }

    public static bool IsSymbolsRequest(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var lower = input.ToLowerInvariant();
        return SymbolsTerms.Any(term => lower.Contains(term));
    }

    public static bool IsCodeQuery(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var lower = input.ToLowerInvariant();

        if (ExplicitCpTerms.Any(term => lower.Contains(term)))    return false;
        if (ExplicitCocoTerms.Any(term => lower.Contains(term)))  return true;

        var matchCount = CodeKeywords.Count(keyword => lower.Contains(keyword));
        return matchCount >= 2;
    }

    public static bool IsExplicitCocoRequest(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var lower = input.ToLowerInvariant();
        return ExplicitCocoTerms.Any(term => lower.Contains(term));
    }

    public static bool IsExplicitCpRequest(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var lower = input.ToLowerInvariant();
        return ExplicitCpTerms.Any(term => lower.Contains(term));
    }
}
