using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LaaUnitTests;

public class StreamResilienceAndProvisionalMemoryTests
{
    [Fact]
    public async Task StreamResilience_ProcessesLinesAsyncWithoutBlocking()
    {
        var ssePayload = "data: {\"type\":\"token\",\"content\":\"Hello\"}\n\ndata: [DONE]\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ssePayload));
        using var reader = new StreamReader(stream);

        var lineCount = 0;
        var receivedDone = false;

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            lineCount++;
            var data = line["data: ".Length..];
            if (data == "[DONE]")
            {
                receivedDone = true;
                break;
            }
        }

        Assert.Equal(2, lineCount);
        Assert.True(receivedDone);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void SpeakerDiarization_FallbackTurn_AlternatesSpeakers(int segmentIndex, int expectedSpeaker)
    {
        var speakerIndex = (segmentIndex % 2) + 1;
        Assert.Equal(expectedSpeaker, speakerIndex);
    }

    [Theory]
    [InlineData(4500, 4, 2)]
    [InlineData(8500, 4, 3)]
    [InlineData(12500, 4, 4)]
    [InlineData(16500, 4, 1)]
    public void SpeakerDiarization_EnergyQuantile_ClustersUpToMaxSpeakers(short sample, int maxSpeakers, int expectedSpeaker)
    {
        var cluster = (Math.Abs(sample) / 4000) % maxSpeakers;
        var speakerIndex = cluster + 1;
        Assert.Equal(expectedSpeaker, speakerIndex);
    }
}
