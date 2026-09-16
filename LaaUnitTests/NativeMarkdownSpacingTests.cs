namespace LaaUnitTests;

public class NativeMarkdownSpacingTests
{
    [Fact]
    public void NativeMarkdownView_DefaultBlockSpacing_IsNotGlobal()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "NativeMarkdownView.cs.txt"));

        Assert.Contains("public NativeMarkdownView()", source);
        Assert.Contains("Spacing = 0;", source);
        Assert.Contains("SearchResultSpacing.TopGap(rawText)", source);
    }
}
