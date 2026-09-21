using LocalAIAssistant.Knowledge.Inbox;

namespace LaaUnitTests;

public class InboxReturnRefreshPolicyTests
{
    [Fact]
    public void Appearance_FirstVisit_RequestsLoad()
    {
        var policy = new InboxReturnRefreshPolicy();

        Assert.True(policy.ShouldLoadOnAppearance(0));
    }

    [Fact]
    public void Appearance_ReadOnlyDetailReturn_ReusesListOnlyOnce()
    {
        var policy = new InboxReturnRefreshPolicy();
        policy.MarkLoaded(0);
        policy.BeginDetailNavigation();

        Assert.False(policy.ShouldLoadOnAppearance(0));
        Assert.True(policy.ShouldLoadOnAppearance(0));
    }

    [Fact]
    public void Appearance_ReturnAfterSuccessfulEdit_RequestsLoad()
    {
        var state = new KnowledgeInboxRefreshState();
        var policy = new InboxReturnRefreshPolicy();
        policy.MarkLoaded(state.Revision);
        policy.BeginDetailNavigation();
        state.MarkChanged();

        Assert.True(policy.ShouldLoadOnAppearance(state.Revision));
    }

    [Fact]
    public void Appearance_CanceledNavigation_DoesNotSkipNormalLoad()
    {
        var policy = new InboxReturnRefreshPolicy();
        policy.MarkLoaded(0);
        policy.BeginDetailNavigation();
        policy.CancelDetailNavigation();

        Assert.True(policy.ShouldLoadOnAppearance(0));
    }

    [Fact]
    public void Appearance_NoSuccessfulLoad_DoesNotReuseFailedList()
    {
        var policy = new InboxReturnRefreshPolicy();
        policy.BeginDetailNavigation();

        Assert.True(policy.ShouldLoadOnAppearance(0));
    }

    [Fact]
    public void Appearance_ChangeDuringLoad_RemainsInvalidated()
    {
        var state = new KnowledgeInboxRefreshState();
        var revisionAtLoadStart = state.Revision;
        state.MarkChanged();
        var policy = new InboxReturnRefreshPolicy();
        policy.MarkLoaded(revisionAtLoadStart);
        policy.BeginDetailNavigation();

        Assert.True(policy.ShouldLoadOnAppearance(state.Revision));
    }

    [Fact]
    public async Task SuccessfulMutation_Refreshes_Registered_Inbox_Before_Return()
    {
        var state = new KnowledgeInboxRefreshState();
        var refreshCount = 0;
        state.RegisterRefresh(() =>
        {
            refreshCount++;
            return Task.CompletedTask;
        });

        await state.MarkChangedAndRefreshAsync();

        Assert.Equal(1, state.Revision);
        Assert.Equal(1, refreshCount);
    }

    [Fact]
    public async Task SuccessfulMutation_Without_Registered_Inbox_Still_Invalidates()
    {
        var state = new KnowledgeInboxRefreshState();

        await state.MarkChangedAndRefreshAsync();

        Assert.Equal(1, state.Revision);
    }
}
