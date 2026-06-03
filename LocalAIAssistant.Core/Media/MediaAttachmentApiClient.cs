using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LocalAIAssistant.Core.Media;

public sealed class MediaAttachmentApiClient : IMediaAttachmentApiClient
{
    private readonly HttpClient _http;

    public MediaAttachmentApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<MediaAttachmentDto?> UploadAsync( Guid              journalId
                                                      , string            fileName
                                                      , string            contentType
                                                      , Stream            stream
                                                      , CancellationToken ct = default )
    {
        using var content    = new MultipartFormDataContent();
        var       filePart   = new StreamContent(stream);
        filePart.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(filePart, "file", fileName);

        try
        {
            var response = await _http.PostAsync($"api/journals/{journalId}/media", content, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<MediaAttachmentDto>(cancellationToken: ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException)       { return null; }
    }

    public async Task<IReadOnlyList<MediaAttachmentDto>?> ListAsync( Guid              journalId
                                                                   , CancellationToken ct = default )
    {
        try
        {
            var response = await _http.GetAsync($"api/journals/{journalId}/media", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<IReadOnlyList<MediaAttachmentDto>>(cancellationToken: ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException)       { return null; }
    }

    public async Task<bool> DeleteAsync( Guid              id
                                       , CancellationToken ct = default )
    {
        try
        {
            var response = await _http.DeleteAsync($"api/media/{id}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException)       { return false; }
    }

    public async Task<Stream?> DownloadAsync( Guid              id
                                            , CancellationToken ct = default )
    {
        try
        {
            var response = await _http.GetAsync($"api/media/{id}/file", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException)       { return null; }
    }
}
