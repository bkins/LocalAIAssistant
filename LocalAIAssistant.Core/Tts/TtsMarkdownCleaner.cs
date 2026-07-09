namespace LocalAIAssistant.Core.Tts;

public static class TtsMarkdownCleaner
{
    /// <summary>
    /// Removes common markdown formatting characters that would be spoken literally by TTS.
    /// </summary>
    public static string StripMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = text.Replace("###", "").Replace("##", "").Replace("# ", "")
                          .Replace("**", "").Replace("__", "")
                          .Replace("> ", "").Replace("`", "");
        return cleaned.Trim();
    }
}
