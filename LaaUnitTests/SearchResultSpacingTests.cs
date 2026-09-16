using LocalAIAssistant.Views.Controls;

namespace LaaUnitTests;

public class SearchResultSpacingTests
{
    [Fact]
    public void TopGap_ParsedReferenceLinkHeader_UsesOriginalMarkdown()
    {
        var markdown = "Results for 'joe':\n\n[journal] entry #1 (score: 0.92)\n\n# Title\n\nBody.\n\n[journal] entry #2 (score: 0.81)\n\n# Another title\n\nAnother body.\n\n[journal]: journal";
        var document = Markdig.Markdown.Parse(markdown);
        var paragraphs = document.OfType<Markdig.Syntax.ParagraphBlock>().ToArray();

        Assert.Contains(paragraphs, paragraph => paragraph.Inline!.Any(inline => inline is Markdig.Syntax.Inlines.LinkInline));
        Assert.Equal(2, paragraphs.Count(paragraph => SearchResultSpacing.TopGap(markdown.Substring(paragraph.Span.Start, paragraph.Span.Length)) > 0));
    }
    [Theory]
    [InlineData("[journal] entry #8 (score: 0.92)", 12)]
    [InlineData("[task] task #1 (score: 0,92)\n  Snippet", 12)]
    [InlineData("[journal] entry #8 (score: 0.92)\r\n  Snippet", 12)]
    [InlineData("Results for 'joe':", 0)]
    [InlineData("A journal paragraph about Joe.", 0)]
    [InlineData("# Journal title", 0)]
    [InlineData("Body mentioning [journal] entry #8 (score: 0.92)", 0)]
    [InlineData("", 0)]
    public void TopGap_OnlyResultHeaders_AddsSeparation(string text, double expected)
        => Assert.Equal(expected, SearchResultSpacing.TopGap(text));
}
