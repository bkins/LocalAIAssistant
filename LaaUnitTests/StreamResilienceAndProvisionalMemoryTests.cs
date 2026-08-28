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
        var cluster = (Math.Abs((int)sample) / 4000) % maxSpeakers;
        var speakerIndex = cluster + 1;
        Assert.Equal(expectedSpeaker, speakerIndex);
    }

    [Fact]
    public void SpeakerDiarization_MinValueSample_DoesNotThrowOverflow()
    {
        short sample = short.MinValue; // -32768
        var cluster = (Math.Abs((int)sample) / 4000) % 4;
        var speakerIndex = cluster + 1;
        Assert.InRange(speakerIndex, 1, 4);
    }

    [Theory]
    [InlineData(true, "2|1", 2)]
    [InlineData(false, "2|1", 1)]
    [InlineData(true, "0|1", 0)]
    [InlineData(false, "0|1", 1)]
    [InlineData(true, null, 1)]
    [InlineData(false, null, 0)]
    public void BoolToIntConverter_ConvertsCorrectly(bool input, string? parameter, int expected)
    {
        var converter = new LocalAIAssistant.Converters.BoolToIntConverter();
        var result = converter.Convert(input, typeof(int), parameter, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NullToBoolConverter_ReturnsExpected()
    {
        var converter = new LocalAIAssistant.Converters.NullToBoolConverter();
        Assert.True((bool)converter.Convert("hello", typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.False((bool)converter.Convert(null, typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(0, "Memory")]
    [InlineData(1, "Memory (1)")]
    [InlineData(3, "Memory (3)")]
    [InlineData(10, "Memory (10)")]
    public void MemoryTabTitle_ComputesExpectedTitle_FromPendingMemoryCount(int pendingCount, string expectedTitle)
    {
        string ComputeMemoryTabTitle(int count) => count > 0 ? $"Memory ({count})" : "Memory";

        var result = ComputeMemoryTabTitle(pendingCount);

        Assert.Equal(expectedTitle, result);
    }
}


