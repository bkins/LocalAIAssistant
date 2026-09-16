using System.Text.Json;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

namespace LaaUnitTests;

public class ConversationStreamPayloadTests
{
    [Theory]
    [InlineData("Results for 'joe':\r\n\r\n[journal] entry #1\n  Memory.")]
    [InlineData("\n\n")]
    [InlineData(" next token ")]
    [InlineData("")]
    [InlineData("\"quoted\" \\ text 😀")]
    public void Decode_JsonString_PreservesExactChunk(string chunk)
    {
        var payload = JsonSerializer.Serialize(chunk);

        var result = ConversationStreamPayload.Decode(payload, true);

        Assert.Equal(chunk, result);
    }

    [Fact]
    public void Decode_LegacyPayload_DoesNotInterpretJsonOrWhitespace()
        => Assert.Equal(" \"quoted\" ", ConversationStreamPayload.Decode(" \"quoted\" ", false));

    [Theory]
    [InlineData("null")]
    [InlineData("not json")]
    public void Decode_InvalidNegotiatedPayload_ThrowsInsteadOfDroppingContent(string payload)
        => Assert.Throws<JsonException>(() => ConversationStreamPayload.Decode(payload, true));
}
