namespace LocalAIAssistant.Core.Coco;

/// <summary>
/// Heuristic that decides whether clipboard text looks like code.
/// Strong structural tokens ({, =>) are sufficient alone.
/// Code keywords (class, void, function, def, …) are each sufficient alone.
/// The combination of ( with ; is treated as a code pattern.
/// Soft keywords (var, let, return) require 2+ to qualify.
/// </summary>
public static class ClipboardCodeDetector
{
    private static readonly string[] StrongKeywords =
    {
        "class ", "void ", "def ", "function ", "namespace "
      , "public ", "private ", "protected ", "import ", "async "
    };

    private static readonly string[] SoftKeywords =
    {
        "var ", "let ", "const ", "return "
    };

    public static bool IsCode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (text.Contains('{'))  return true;
        if (text.Contains("=>")) return true;

        var lower = text.ToLowerInvariant();

        if (StrongKeywords.Any(keyword => lower.Contains(keyword))) return true;

        if (text.Contains('(') && text.Contains(';')) return true;

        var softCount = SoftKeywords.Count(keyword => lower.Contains(keyword));
        return softCount >= 2;
    }
}
