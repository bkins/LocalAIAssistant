namespace LaaUnitTests;

public class LogsPageInteractionTests
{
    [Fact]
    public void Date_Filter_And_Detail_Navigation_Are_Discoverable_And_Explicit()
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogsPage.xaml.txt"));
        var codeBehind = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogsPage.xaml.cs.txt"));

        Assert.Contains("Text=\"Filter by date range\"", markup);
        Assert.Contains("AutomationId=\"ViewLogDetailsButton\"", markup);
        Assert.Contains("Clicked=\"OnViewDetailsClicked\"", markup);
        Assert.Contains("GoToAsync(nameof(LogDetailPage)", codeBehind);
    }
}
