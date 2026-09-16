using LocalAIAssistant.Views.Controls;

namespace LaaUnitTests;

public class SearchResultSpacingTests
{
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
