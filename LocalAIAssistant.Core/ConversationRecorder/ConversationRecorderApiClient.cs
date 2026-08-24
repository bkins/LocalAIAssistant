using System.Net.Http.Json;
using System.Text.Json;

namespace LocalAIAssistant.Core.ConversationRecorder;

public class ConversationRecorderApiClient : IConversationRecorderApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ConversationRecorderApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TranscriptDto?> TranscribeRecordingAsync( Guid conversationId
                                                              , Stream audioStream
                                                              , string mimeType = "audio/wav"
                                                              , CancellationToken cancellationToken = default )
    {
        try
        {
            using var content = new StreamContent(audioStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

            var response = await _httpClient.PostAsync($"api/recorder/conversations/{conversationId}/transcribe", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TranscriptDto>(_jsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<TranscriptDto?> GetTranscriptAsync( Guid conversationId, CancellationToken cancellationToken = default )
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/recorder/conversations/{conversationId}/transcript", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TranscriptDto>(_jsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
