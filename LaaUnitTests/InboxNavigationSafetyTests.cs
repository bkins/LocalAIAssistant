namespace LaaUnitTests;

public class InboxNavigationSafetyTests
{
    [Fact]
    public void InboxOpen_UnsupportedKinds_DoesNotThrowOutOfRange()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "KnowledgeInboxViewModel.cs.txt"));

        Assert.DoesNotContain("throw new ArgumentOutOfRangeException", source);
        Assert.Contains("await KnowledgeItemDetailNavigation.OpenAsync", source);
        Assert.Contains("HasError = error is not null", source);
    }
}
