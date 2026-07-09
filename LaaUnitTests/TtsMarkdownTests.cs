using LocalAIAssistant.Core.Tts;

namespace LaaUnitTests;

public class TtsMarkdownTests
{
    [Theory]
    [InlineData("### Heading 3", "Heading 3")]
    [InlineData("## Heading 2", "Heading 2")]
    [InlineData("# Heading 1", "Heading 1")]
    [InlineData("**Bold text**", "Bold text")]
    [InlineData("__Bold text__", "Bold text")]
    [InlineData("> Blockquote", "Blockquote")]
    [InlineData("`inline code`", "inline code")]
    [InlineData("Hello **world** from `antigravity`", "Hello world from antigravity")]
    public void StripMarkdown_RemovesCommonFormatting(string input, string expected)
    {
        var result = TtsMarkdownCleaner.StripMarkdown(input);
        Assert.Equal(expected, result);
    }
}
