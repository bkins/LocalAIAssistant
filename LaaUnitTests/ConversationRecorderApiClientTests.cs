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

    [Fact]
    public async Task DiarizeRecordingAsync_ReturnsDiarizedTranscript_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedTranscript = new TranscriptDto
        {
            Id             = Guid.NewGuid()
          , ConversationId = conversationId
          , Status         = "Completed"
          , Segments       = new List<TranscriptSegmentDto>
            {
                new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(5), Text = "Turn 1", SpeakerLabel = "Speaker 1" }
              , new() { Start = TimeSpan.FromSeconds(5), End = TimeSpan.FromSeconds(10), Text = "Turn 2", SpeakerLabel = "Speaker 2" }
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedTranscript));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var result = await apiClient.DiarizeRecordingAsync(conversationId, audioStream);

        Assert.NotNull(result);
        Assert.Equal(2, result.Segments.Count);
        Assert.Equal("Speaker 1", result.Segments[0].SpeakerLabel);
        Assert.Equal("Speaker 2", result.Segments[1].SpeakerLabel);
    }

    [Fact]
    public async Task MapParticipantsAsync_ReturnsUpdatedTranscript_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedTranscript = new TranscriptDto
        {
            Id             = Guid.NewGuid()
          , ConversationId = conversationId
          , Status         = "Completed"
          , Segments       = new List<TranscriptSegmentDto>
            {
                new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(5), Text = "Turn 1", SpeakerId = "Speaker 1", SpeakerLabel = "Ben" }
              , new() { Start = TimeSpan.FromSeconds(5), End = TimeSpan.FromSeconds(10), Text = "Turn 2", SpeakerId = "Speaker 2", SpeakerLabel = "Sarah" }
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedTranscript));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var speakerMap = new Dictionary<string, string>
        {
            ["Speaker 1"] = "Ben"
          , ["Speaker 2"] = "Sarah"
        };
        var result = await apiClient.MapParticipantsAsync(conversationId, speakerMap);

        Assert.NotNull(result);
        Assert.Equal(2, result.Segments.Count);
        Assert.Equal("Ben", result.Segments[0].SpeakerLabel);
        Assert.Equal("Sarah", result.Segments[1].SpeakerLabel);
    }

    [Fact]
    public async Task GetConversationDetailsAsync_ReturnsDetails_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedDetails = new ConversationDetailsDto
        {
            Record       = new ConversationRecordDto { Id = conversationId, Title = "Library Test" }
          , Transcript   = new TranscriptDto { ConversationId = conversationId }
          , Participants = new List<ConversationParticipantDto> { new() { ConversationId = conversationId, SpeakerId = "Speaker 1", DisplayName = "Ben" } }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedDetails));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.GetConversationDetailsAsync(conversationId);

        Assert.NotNull(result);
        Assert.Equal(conversationId, result.Record.Id);
        Assert.Single(result.Participants);
    }

    [Fact]
    public async Task SearchConversationsAsync_ReturnsFilteredRecords_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedRecords = new List<ConversationRecordDto>
        {
            new() { Id = conversationId, Title = "Found Title" }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedRecords));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var results = await apiClient.SearchConversationsAsync(query: "Found", participant: "Ben");

        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Found Title", results[0].Title);
    }

    [Fact]
    public async Task AnalyzeConversationAsync_ReturnsAnalysis_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedAnalysis = new ConversationAnalysisDto
        {
            Id             = Guid.NewGuid()
          , ConversationId = conversationId
          , Summary        = "Meeting agreed to proceed with design."
          , Status         = "Completed"
          , Topics         = new List<AnalysisDerivedItemDto>
            {
                new() { Type = "Topic", Content = "API Architecture" }
            }
          , ActionItems    = new List<AnalysisDerivedItemDto>
            {
                new() { Type = "ActionItem", Content = "Implement endpoints" }
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedAnalysis));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.AnalyzeConversationAsync(conversationId);

        Assert.NotNull(result);
        Assert.Equal(conversationId, result.ConversationId);
        Assert.Equal("Completed", result.Status);
        Assert.Equal("Meeting agreed to proceed with design.", result.Summary);
        Assert.Single(result.Topics);
        Assert.Single(result.ActionItems);
    }

    [Fact]
    public async Task GetAnalysisAsync_ReturnsAnalysis_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedAnalysis = new ConversationAnalysisDto
        {
            Id             = Guid.NewGuid()
          , ConversationId = conversationId
          , Summary        = "Cached analysis summary."
          , Status         = "Completed"
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedAnalysis));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.GetAnalysisAsync(conversationId);

        Assert.NotNull(result);
        Assert.Equal(conversationId, result.ConversationId);
        Assert.Equal("Cached analysis summary.", result.Summary);
    }

    [Fact]
    public async Task ExtractMemoriesAsync_ReturnsList_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedMemories = new List<ConversationMemoryCandidateDto>
        {
            new()
            {
                Id             = Guid.NewGuid()
              , ConversationId = conversationId
              , Category       = "Fact"
              , Content        = "Sarah joined as lead."
              , Speaker        = "Sarah"
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedMemories));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.ExtractMemoriesAsync(conversationId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Fact", result[0].Category);
        Assert.Equal("Sarah joined as lead.", result[0].Content);
    }

    [Fact]
    public async Task GetMemoriesAsync_ReturnsList_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedMemories = new List<ConversationMemoryCandidateDto>
        {
            new()
            {
                Id             = Guid.NewGuid()
              , ConversationId = conversationId
              , Category       = "Commitment"
              , Content        = "Ben will design schema."
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedMemories));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.GetMemoriesAsync(conversationId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Commitment", result[0].Category);
    }

    [Fact]
    public async Task ConfirmMemoryAsync_ReturnsTrue_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var memoryId       = Guid.NewGuid();

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.ConfirmMemoryAsync(conversationId, memoryId);

        Assert.True(result);
    }

    [Fact]
    public async Task QueryMemoriesAsync_ReturnsMatchingList_WhenApiReturnsSuccess()
    {
        var expectedMemories = new List<ConversationMemoryCandidateDto>
        {
            new()
            {
                Id       = Guid.NewGuid()
              , Category = "Fact"
              , Content  = "Database uses SQLite."
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedMemories));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.QueryMemoriesAsync("SQLite");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Database uses SQLite.", result[0].Content);
    }

    [Fact]
    public async Task ProcessCopilotSliceAsync_ReturnsSliceResult_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedResult = new CopilotSliceResultDto
        {
            ConversationId  = conversationId
          , SliceIndex      = 1
          , TranscribedText = "Do you remember Sarah's dog's name?"
          , Insights        = new List<CopilotInsightDto>
                              {
                                  new()
                                  {
                                      Id             = Guid.NewGuid()
                                    , ConversationId = conversationId
                                    , InsightType    = "RecallHint"
                                    , Headline       = "Memory Recall: Sarah's dog"
                                    , Detail         = "Milo (Golden Retriever)"
                                  }
                              }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResult));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var result = await apiClient.ProcessCopilotSliceAsync(conversationId, audioStream, 1, 15.0, 15.0, "Previous sentence.");

        Assert.NotNull(result);
        Assert.Equal(conversationId, result.ConversationId);
        Assert.True(result.HasActionableInsight);
        Assert.Single(result.Insights);
        Assert.Equal("Memory Recall: Sarah's dog", result.Insights[0].Headline);
    }

    [Fact]
    public async Task GetCopilotInsightsAsync_ReturnsInsightsList_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var expectedInsights = new List<CopilotInsightDto>
        {
            new()
            {
                Id             = Guid.NewGuid()
              , ConversationId = conversationId
              , Headline       = "Insight 1"
              , Detail         = "Detail 1"
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(expectedInsights));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.GetCopilotInsightsAsync(conversationId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Insight 1", result[0].Headline);
    }

    [Fact]
    public async Task DismissCopilotInsightAsync_ReturnsTrue_WhenApiReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var insightId = Guid.NewGuid();

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{\"status\":\"Dismissed\"}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5273/") };
        var apiClient = new ConversationRecorderApiClient(client);

        var result = await apiClient.DismissCopilotInsightAsync(conversationId, insightId);

        Assert.True(result);
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
