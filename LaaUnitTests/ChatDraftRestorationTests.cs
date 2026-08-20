using LocalAIAssistant.Data;
using Xunit;

namespace LaaUnitTests;

public class ChatDraftRestorationTests
{
    [Fact]
    public void ChatDraftPromptPrefKey_HasExpectedValue()
    {
        var expectedKey = "ChatDraftPrompt";

        var actualKey = StringConsts.ChatDraftPromptPrefKey;

        Assert.Equal(expectedKey, actualKey);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("Listening... Speak now!", true)]
    [InlineData("Hello, can you help me with tasks?", false)]
    public void DraftValidation_IdentifiesTransientOrEmptyDrafts(string input, bool expectedShouldClear)
    {
        var shouldClear = input.HasNoValue()
                       || input.EqualsIgnoreCase("Listening... Speak now!");

        Assert.Equal(expectedShouldClear, shouldClear);
    }
}
