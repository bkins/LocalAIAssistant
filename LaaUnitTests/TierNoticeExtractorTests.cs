using LocalAIAssistant.Core.Conversation;

namespace LaaUnitTests;

public class TierNoticeExtractorTests
{
    [Fact]
    public void Extract_ReturnsUnchanged_WhenNoItalicLastLine()
    {
        var input = "This is a normal response without any notice.";

        var (clean, notice) = TierNoticeExtractor.Extract(input);

        Assert.Equal(input, clean);
        Assert.Null(notice);
    }

    [Fact]
    public void Extract_DetectsAsteriskItalic_ModelDowngrade()
    {
        var input = "Here is the answer.\n\n*Note: Model downgraded to llama-3.1-8b-instant*";

        var (clean, notice) = TierNoticeExtractor.Extract(input);

        Assert.Equal("Here is the answer.", clean);
        Assert.Equal("Note: Model downgraded to llama-3.1-8b-instant", notice);
    }

    [Fact]
    public void Extract_DetectsUnderscoreItalic_UsingInsteadOf()
    {
        var input = "Response text.\n\n_Note: Using gemma2 instead of qwen_";

        var (clean, notice) = TierNoticeExtractor.Extract(input);

        Assert.Equal("Response text.", clean);
        Assert.Equal("Note: Using gemma2 instead of qwen", notice);
    }

    [Fact]
    public void Extract_DetectsFallbackNotice()
    {
        var input = "The answer is 42.\n*Note: Fallback model used*";

        var (clean, notice) = TierNoticeExtractor.Extract(input);

        Assert.Equal("The answer is 42.", clean);
        Assert.Equal("Note: Fallback model used", notice);
    }

    [Fact]
    public void Extract_IgnoresItalicNotRelatedToTier()
    {
        var input = "Response text.\n\n*This is just emphasis*";

        var (clean, notice) = TierNoticeExtractor.Extract(input);

        Assert.Equal(input, clean);
        Assert.Null(notice);
    }

    [Fact]
    public void Extract_ReturnsUnchanged_WhenMessageIsEmpty()
    {
        var (clean, notice) = TierNoticeExtractor.Extract(string.Empty);

        Assert.Equal(string.Empty, clean);
        Assert.Null(notice);
    }

    [Fact]
    public void Extract_ReturnsUnchanged_WhenNoNewline()
    {
        var input = "*Note: Model tier downgraded*";

        var (clean, notice) = TierNoticeExtractor.Extract(input);

        Assert.Equal(input, clean);
        Assert.Null(notice);
    }

    [Fact]
    public void Extract_TrimsTrailingWhitespace_BeforeParsingLastLine()
    {
        var input = "The answer.\n\n*Note: Tier downgraded*  \n  ";

        var (clean, notice) = TierNoticeExtractor.Extract(input);

        Assert.Equal("The answer.", clean);
        Assert.Equal("Note: Tier downgraded", notice);
    }
}
