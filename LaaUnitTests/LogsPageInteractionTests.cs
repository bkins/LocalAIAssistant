namespace LaaUnitTests;

public class LogsPageInteractionTests
{
    [Fact]
    public void Date_Filter_And_Detail_Navigation_Are_Discoverable_And_Explicit()
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogsPage.xaml.txt"));
        var codeBehind = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogsPage.xaml.cs.txt"));
        var detailMarkup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogDetailPage.xaml.txt"));

        Assert.Contains("Text=\"Filter by date range\"", markup);
        Assert.Contains("WidthRequest=\"52\"", markup);
        Assert.Contains("AutomationId=\"ViewLogDetailsButton\"", markup);
        Assert.Contains("Tapped=\"OnViewDetailsTapped\"", markup);
        Assert.Contains("Text=\"Details ›\"", markup);
        Assert.DoesNotContain("<Button Grid.Row=\"3\"", markup);
        Assert.DoesNotContain("StaticResource Gray800", detailMarkup);
        Assert.Contains("GoToAsync(nameof(LogDetailPage)", codeBehind);
    }
}
