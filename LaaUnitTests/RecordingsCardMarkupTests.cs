namespace LaaUnitTests;

public sealed class RecordingsCardMarkupTests
{
    [Fact]
    public void RecordingCard_SeparatesMetadataFromTouchFriendlyActionBar()
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "RecordingsPage.xaml.txt"));

        Assert.DoesNotContain("ColumnDefinitions=\"*,Auto,Auto,Auto\"", markup);
        Assert.Contains("ColumnDefinitions=\"*,*,*\"", markup);
        Assert.Contains("Path=PlayRecordingCommand", markup);
        Assert.Contains("Path=StopPlaybackCommand", markup);
        Assert.Contains("Path=TranscribeAndDiarizeCommand", markup);
        Assert.Contains("Path=ToggleTranscriptCommand", markup);
        Assert.Contains("Android=48", markup);
    }
}
