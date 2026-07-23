using LocalAIAssistant.Services.Interfaces;
using Xunit;

namespace LaaUnitTests;

public class QueueProcessedEventArgsTests
{
    [Fact]
    public void Constructor_SetsReplayedCount_Correctly()
    {
        var count = 5;

        var args = new QueueProcessedEventArgs(count);

        Assert.Equal(5, args.ReplayedCount);
    }
}
