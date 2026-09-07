namespace LaaUnitTests;

public sealed class ChatTouchTargetMarkupTests
{
    [Fact]
    public void AndroidChatControls_Use48DpTouchTargets()
    {
        var markup = ReadMainPageMarkup();

        Assert.Contains("AutomationId=\"VoiceButton\"", markup);
        Assert.Contains("AutomationId=\"SendButton\"", markup);
        Assert.Contains("AutomationId=\"StopButton\"", markup);
        Assert.Contains("AutomationId=\"ClearButton\"", markup);
        Assert.Contains("Android=48", markup);
    }

    [Fact]
    public void MessageCopy_UsesDedicatedActionInsteadOfFullBubbleTap()
    {
        var markup = ReadMainPageMarkup();

        Assert.DoesNotContain("Tapped=\"OnMessageTapped\"", markup);
        Assert.Contains("Clicked=\"OnCopyMessageClicked\"", markup);
        Assert.Contains("SemanticProperties.Description=\"Copy message\"", markup);
    }

    [Fact]
    public void ThinkingMessage_UsesSpinnerAndHidesMarkdownUntilContentArrives()
    {
        var markup = ReadMainPageMarkup();

        Assert.Contains("IsVisible=\"{Binding IsThinking}\"", markup);
        Assert.Contains("IsRunning=\"{Binding IsThinking}\"", markup);
        Assert.Contains("Text=\"Thinking…\"", markup);
        Assert.Contains("IsVisible=\"{Binding IsThinking, Converter={StaticResource InverseBoolConverter}}\"", markup);
        Assert.Contains("SemanticProperties.Description=\"Copy message\"\n                                            IsVisible=\"{Binding IsThinking, Converter={StaticResource InverseBoolConverter}}\"", markup);
    }

    private static string ReadMainPageMarkup()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainPage.xaml.txt");
        return File.ReadAllText(path);
    }
}
