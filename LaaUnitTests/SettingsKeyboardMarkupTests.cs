namespace LaaUnitTests;

public sealed class SettingsKeyboardMarkupTests
{
    [Fact]
    public void UrlSettings_UseUrlKeyboardAndBackgroundDismissal()
    {
        var markup = ReadSettingsMarkup();

        Assert.Contains("x:Name=\"EndpointEntry\"", markup);
        Assert.Contains("x:Name=\"CocoBaseUrlEntry\"", markup);
        Assert.Contains("Keyboard=\"Url\"", markup);
        Assert.Contains("Tapped=\"OnSettingsBackgroundTapped\"", markup);
    }

    private static string ReadSettingsMarkup()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SettingsPage.xaml.txt");
        return File.ReadAllText(path);
    }
}
