using LocalAIAssistant.Views;

namespace LaaUnitTests;

public sealed class ChatBottomPinningTests
{
    [Fact]
    public void ScrollState_StopsFollowing_WhenNewestMessageLeavesViewport()
    {
        var state = new ChatScrollState();

        state.ObserveViewport(lastVisibleItemIndex: 3, messageCount: 8);

        Assert.False(state.IsFollowingLatest);
        Assert.False(state.ShouldPinAfterContentChange);
    }

    [Fact]
    public void ScrollState_ResumesFollowing_WhenNewestMessageReturnsToViewport()
    {
        var state = new ChatScrollState();
        state.ObserveViewport(lastVisibleItemIndex: 3, messageCount: 8);

        state.ObserveViewport(lastVisibleItemIndex: 7, messageCount: 8);

        Assert.True(state.IsFollowingLatest);
        Assert.True(state.ShouldPinAfterContentChange);
    }

    [Fact]
    public void ScrollState_NewPromptRestoresFollowing()
    {
        var state = new ChatScrollState();
        state.ObserveViewport(lastVisibleItemIndex: 3, messageCount: 8);

        state.MarkPromptSent();

        Assert.True(state.IsFollowingLatest);
    }

    [Fact]
    public void MainPage_WiresViewportAndRenderedContentSignals()
    {
        var markupPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainPage.xaml.txt");
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MainPage.xaml.cs.txt");
        var markup     = File.ReadAllText(markupPath);
        var source     = File.ReadAllText(sourcePath);

        Assert.Contains("Scrolled=\"OnMessagesViewScrolled\"", markup);
        Assert.Contains("SizeChanged=\"OnMessageContentSizeChanged\"", markup);
        Assert.Contains("ScheduleBottomPin", source);
        Assert.Contains("MarkPromptSent", source);
    }
}
