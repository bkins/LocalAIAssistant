namespace LocalAIAssistant.Core.Conversation;

/// <summary>
/// Parses the trailing model-tier-downgrade notice that the CP API may append to a response
/// as an italic last line (e.g. *Note: Using llama-3.1-8b-instant instead of gemma2-9b-it*).
/// Returns the clean message body and the extracted notice separately so the UI can show
/// the notice as a transient chip rather than mixing it into the response text.
/// </summary>
public static class TierNoticeExtractor
{
    public static (string CleanMessage, string? Notice) Extract(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return (message, null);

        var trimmed     = message.TrimEnd();
        var lastNewline = trimmed.LastIndexOf('\n');

        if (lastNewline < 0)
            return (message, null);

        var lastLine = trimmed[(lastNewline + 1)..].Trim();

        if (!IsEntirelyItalic(lastLine) || !LooksLikeTierNotice(lastLine))
            return (message, null);

        var notice    = StripItalicMarkers(lastLine);
        var cleanBody = trimmed[..lastNewline].TrimEnd();

        return (cleanBody, notice);
    }

    private static bool IsEntirelyItalic(string line)
        => line.Length > 2
        && ((line[0] == '*' && line[^1] == '*')
         || (line[0] == '_' && line[^1] == '_'));

    private static bool LooksLikeTierNotice(string line)
    {
        var inner = StripItalicMarkers(line).ToLowerInvariant();
        return inner.Contains("model")
            || inner.Contains("tier")
            || inner.Contains("note:")
            || inner.Contains("fallback")
            || inner.Contains("downgrad")
            || inner.Contains("using ")
            || inner.Contains("instead of");
    }

    private static string StripItalicMarkers(string line)
        => line[1..^1];
}
