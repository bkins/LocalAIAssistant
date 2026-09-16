namespace LaaUnitTests;

public class InboxReturnSafetyTests
{
    [Fact]
    public void PageAppearance_UsesReturnAwareLoading()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "KnowledgeInboxPage.xaml.cs.txt"));

        Assert.DoesNotContain("await ViewModel.LoadAsync();", source);
        Assert.Contains("await ViewModel.LoadOnAppearingAsync();", source);
        var viewModel = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "KnowledgeInboxViewModel.cs.txt"));
        Assert.Contains("_returnRefreshPolicy.ShouldLoadOnAppearance(_refreshState.Revision)", viewModel);
        Assert.Contains("_returnRefreshPolicy.BeginDetailNavigation()", viewModel);
        Assert.Contains("_returnRefreshPolicy.CancelDetailNavigation()", viewModel);
        Assert.Contains("_returnRefreshPolicy.MarkLoaded(revision)", viewModel);
        Assert.DoesNotContain("_localStore.List()", viewModel);
    }
}
