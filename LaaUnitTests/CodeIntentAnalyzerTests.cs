using LocalAIAssistant.Core.Coco;

namespace LaaUnitTests;

public class CodeIntentAnalyzerTests
{
    // ── IsCodeQuery ───────────────────────────────────────────────────────────

    [Fact]
    public void IsCodeQuery_ReturnsFalse_WhenInputIsEmpty()
    {
        Assert.False(CodeIntentAnalyzer.IsCodeQuery(string.Empty));
        Assert.False(CodeIntentAnalyzer.IsCodeQuery("   "));
    }

    [Theory]
    [InlineData("ask coco: what does UserService do?")]
    [InlineData("use coco — explain the auth flow")]
    [InlineData("coco: where is the repository pattern used?")]
    public void IsCodeQuery_ReturnsTrue_WhenExplicitCocoTermPresent(string input)
    {
        Assert.True(CodeIntentAnalyzer.IsCodeQuery(input));
    }

    [Theory]
    [InlineData("ask cp about my tasks")]
    [InlineData("use cp to answer this")]
    [InlineData("ask cognitive platform about my journal")]
    [InlineData("ask personal AI this question")]
    [InlineData("cp: what are my tasks today?")]
    public void IsCodeQuery_ReturnsFalse_WhenExplicitCpTermPresent(string input)
    {
        Assert.False(CodeIntentAnalyzer.IsCodeQuery(input));
    }

    [Fact]
    public void IsCodeQuery_ReturnsTrue_WhenTwoOrMoreCodeKeywordsPresent()
    {
        const string input = "what does the async method return when the Task<string> completes?";

        Assert.True(CodeIntentAnalyzer.IsCodeQuery(input));
    }

    [Fact]
    public void IsCodeQuery_ReturnsFalse_WhenOnlyOneCodeKeywordPresent()
    {
        const string input = "what does my calendar look like today?";

        Assert.False(CodeIntentAnalyzer.IsCodeQuery(input));
    }

    [Fact]
    public void IsCodeQuery_ReturnsFalse_WhenNoCodeKeywordsPresent()
    {
        const string input = "add a task to buy groceries";

        Assert.False(CodeIntentAnalyzer.IsCodeQuery(input));
    }

    [Fact]
    public void IsCodeQuery_CpOverride_TakesPrecedenceOverCodeKeywords()
    {
        // Even with multiple code keywords, explicit CP request wins.
        const string input = "ask cp about the async method and Task<string> issue";

        Assert.False(CodeIntentAnalyzer.IsCodeQuery(input));
    }

    // ── IsExplicitCocoRequest ─────────────────────────────────────────────────

    [Theory]
    [InlineData("ask coco what this method does")]
    [InlineData("use coco for this query")]
    [InlineData("coco: explain this")]
    public void IsExplicitCocoRequest_ReturnsTrue_WhenCocoTermPresent(string input)
    {
        Assert.True(CodeIntentAnalyzer.IsExplicitCocoRequest(input));
    }

    [Theory]
    [InlineData("what does this method do?")]
    [InlineData("ask cp about tasks")]
    [InlineData("")]
    public void IsExplicitCocoRequest_ReturnsFalse_WhenNoCocoTerm(string input)
    {
        Assert.False(CodeIntentAnalyzer.IsExplicitCocoRequest(input));
    }

    // ── IsExplicitCpRequest ───────────────────────────────────────────────────

    [Theory]
    [InlineData("ask cp about today's tasks")]
    [InlineData("use cp for this")]
    [InlineData("ask cognitive platform")]
    [InlineData("ask personal AI")]
    [InlineData("cp: what is on my list?")]
    public void IsExplicitCpRequest_ReturnsTrue_WhenCpTermPresent(string input)
    {
        Assert.True(CodeIntentAnalyzer.IsExplicitCpRequest(input));
    }

    [Theory]
    [InlineData("explain this method")]
    [InlineData("ask coco about the code")]
    [InlineData("")]
    public void IsExplicitCpRequest_ReturnsFalse_WhenNoCpTerm(string input)
    {
        Assert.False(CodeIntentAnalyzer.IsExplicitCpRequest(input));
    }
}
