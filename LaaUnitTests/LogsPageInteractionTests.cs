namespace LaaUnitTests;

public class LogsPageInteractionTests
{
    [Fact]
    public void Date_Filter_And_Detail_Navigation_Are_Discoverable_And_Explicit()
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogsPage.xaml.txt"));
        var codeBehind = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogsPage.xaml.cs.txt"));
        var detailMarkup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogDetailPage.xaml.txt"));
        var detailCodeBehind = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LogDetailPage.xaml.cs.txt"));

        Assert.Contains("Text=\"Filter by date range\"", markup);
        Assert.Contains("WidthRequest=\"52\"", markup);
        Assert.Contains("HandlerChanged=\"OnDateFilterSwitchHandlerChanged\"", markup);
        Assert.Contains("toggleSwitch.MinWidth = 0", codeBehind);
        Assert.Contains("AutomationId=\"ViewLogDetailsButton\"", markup);
        Assert.Contains("<Button Grid.Column=\"4\"", markup);
        Assert.Contains("Clicked=\"OnViewDetailsClicked\"", markup);
        Assert.Contains("MinimumHeightRequest=\"0\"", markup);
        Assert.Contains("Text=\"Details ›\"", markup);
        Assert.DoesNotContain("<Button Grid.Row=\"3\"", markup);
        Assert.DoesNotContain("StaticResource Gray800", detailMarkup);
        Assert.Contains("GoToAsync(nameof(LogDetailPage)", codeBehind);
        Assert.Contains("AutomationId=\"DeleteLogEntryButton\"", detailMarkup);
        Assert.Contains("Clicked=\"OnDeleteEntryClicked\"", detailMarkup);
        Assert.Contains("x:Name=\"DeleteEntryButton\"", detailMarkup);
        Assert.Contains("<Editor Text=\"{Binding Message}\"", detailMarkup);
        Assert.Contains("<Editor Text=\"{Binding Exception}\"", detailMarkup);
        Assert.Contains("<Editor Text=\"{Binding PropertiesFormatted}\"", detailMarkup);
        Assert.Contains("<Editor Text=\"{Binding PrettifiedFullText}\"", detailMarkup);
        Assert.Contains("IsReadOnly=\"True\"", detailMarkup);
        Assert.Contains("DisplayAlert(\"Delete log entry?\"", detailCodeBehind);
        Assert.Contains("DeleteLogEntryAsync", detailCodeBehind);
        Assert.Contains("DeleteEntryButton.Text = \"Deleting...\"", detailCodeBehind);
    }
}
