using System.Net;
using System.Text.Json;
using LocalAIAssistant.Core.ConversationRecorder;
using Xunit;

namespace LaaUnitTests;

public class ConversationRecorderApiClientTests
{
    [Fact]
    public async Task TranscribeRecordingAsync_ReturnsTranscript_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedTranscript = new TranscriptDto
        {
            Id             = Guid.NewGuid()
          , ConversationId = conversationId
          , Status         = "Completed"
          , Segments       = new List<TranscriptSegmentDto>
            {
                new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(5), Text = "Hello world" }
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedTranscript));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var result = await apiClient.TranscribeRecordingAsync(conversationId, audioStream);

        Assert.NotNull(result);
        Assert.Equal(conversationId, result.ConversationId);
        Assert.Equal("Completed", result.Status);
        Assert.Single(result.Segments);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string         _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content    = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
