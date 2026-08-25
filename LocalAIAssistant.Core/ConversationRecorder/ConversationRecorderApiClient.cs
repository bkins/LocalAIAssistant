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

    public async Task<TranscriptDto?> DiarizeRecordingAsync( Guid conversationId
                                                           , Stream audioStream
                                                           , CancellationToken cancellationToken = default )
    {
        try
        {
            using var content = new StreamContent(audioStream);
            var response = await _httpClient.PostAsync($"api/recorder/conversations/{conversationId}/diarize", content, cancellationToken);
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

    public async Task<TranscriptDto?> MapParticipantsAsync( Guid conversationId
                                                           , Dictionary<string, string> speakerMap
                                                           , CancellationToken cancellationToken = default )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/recorder/conversations/{conversationId}/participants", speakerMap, _jsonOptions, cancellationToken);
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

    public async Task<List<ConversationParticipantDto>> GetParticipantsAsync( Guid conversationId, CancellationToken cancellationToken = default )
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/recorder/conversations/{conversationId}/participants", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new List<ConversationParticipantDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<ConversationParticipantDto>>(_jsonOptions, cancellationToken);
            return result ?? new List<ConversationParticipantDto>();
        }
        catch
        {
            return new List<ConversationParticipantDto>();
        }
    }

    public async Task<ConversationDetailsDto?> GetConversationDetailsAsync( Guid conversationId, CancellationToken cancellationToken = default )
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/recorder/conversations/{conversationId}/details", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ConversationDetailsDto>(_jsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ConversationRecordDto>> SearchConversationsAsync( string? query = null
                                                                           , string? participant = null
                                                                           , DateTimeOffset? fromDate = null
                                                                           , DateTimeOffset? toDate = null
                                                                           , CancellationToken cancellationToken = default )
    {
        try
        {
            var queryParams = new List<string>();
            if (query != null)
            {
                queryParams.Add($"q={Uri.EscapeDataString(query)}");
            }
            if (participant != null)
            {
                queryParams.Add($"participant={Uri.EscapeDataString(participant)}");
            }
            if (fromDate.HasValue)
            {
                queryParams.Add($"from={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
            }
            if (toDate.HasValue)
            {
                queryParams.Add($"to={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
            }

            var uri = "api/recorder/conversations/search";
            if (queryParams.Count > 0)
            {
                uri += "?" + string.Join("&", queryParams);
            }

            var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new List<ConversationRecordDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<ConversationRecordDto>>(_jsonOptions, cancellationToken);
            return result ?? new List<ConversationRecordDto>();
        }
        catch
        {
            return new List<ConversationRecordDto>();
        }
    }

    public async Task<bool> UploadAudioAsync( Guid conversationId
                                           , Stream audioStream
                                           , string mimeType = "audio/wav"
                                           , CancellationToken cancellationToken = default )
    {
        try
        {
            using var content = new StreamContent(audioStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(mimeType) ? "audio/wav" : mimeType);

            var response = await _httpClient.PostAsync($"api/recorder/conversations/{conversationId}/audio", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Stream?> GetAudioStreamAsync( Guid conversationId, CancellationToken cancellationToken = default )
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/recorder/conversations/{conversationId}/audio", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
