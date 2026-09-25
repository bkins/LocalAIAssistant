namespace LocalAIAssistant.Views;

public sealed class ChatScrollState
{
    public bool IsFollowingLatest { get; private set; } = true;

    public bool ShouldPinAfterContentChange => IsFollowingLatest;

    public void ObserveViewport(int lastVisibleItemIndex, int messageCount)
    {
        IsFollowingLatest = messageCount == 0 || lastVisibleItemIndex >= messageCount - 1;
    }

    public void MarkPromptSent()
    {
        IsFollowingLatest = true;
    }
}
