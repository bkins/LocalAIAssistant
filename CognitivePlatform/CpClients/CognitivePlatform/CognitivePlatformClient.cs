using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConectivityToApi;
using LocalAIAssistant.CognitivePlatform.DTOs;
using static CP.Client.Core.Intent.FastPathIntentDetector;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

public class CognitivePlatformClient : ICognitivePlatformClient
{
    private readonly HttpClient            _httpClient;
    
    public  IConnectivityReporter Connectivity {get; private set;}

    public CognitivePlatformClient (HttpClient            httpClient
                                  , IConnectivityReporter connectivity)
    {
        _httpClient   = httpClient;
        Connectivity = connectivity;

    }

    public override async Task<ConverseResponseDto> ConverseAsync(string userMessage
                                                                , string conversationId
                                                                , string model)
    {
        try
        {
            var request = BuildRequest(userMessage
                                     , conversationId
                                     , model);

            var response = await _httpClient.PostAsJsonAsync("api/conversation/converse", request);

            response.EnsureSuccessStatusCode();
            
            Connectivity.ReportOnline();
            
            return await response.Content.ReadFromJsonAsync<ConverseResponseDto>()
                ?? new ConverseResponseDto { Message = "(empty response)" };
        }
        catch (HttpRequestException ex)
        {
            Connectivity.ReportOffline(ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Connectivity.ReportOffline(ex);
            throw;
        }
    }

    private static ConverseRequestDto BuildRequest (string userMessage
                                                  , string conversationId
                                                  , string model)
    {

        var isFastPath = IsFastPathIntent(userMessage);

        var request = new ConverseRequestDto
                      {
                              Input     = userMessage
                            , SessionId = conversationId
                            , Model     = model
                            , FastPath  = isFastPath
                            , Streaming = isFastPath.Not()
                      };
        return request;
    }

    public override async IAsyncEnumerable<string> ConverseStreamAsync (string            userMessage
                                                                      , string            conversationId
                                                                      , string            model
                                                                      , [EnumeratorCancellation] CancellationToken ct = default)
    {
        var requestDto = BuildRequest(userMessage
                                    , conversationId
                                    , model);
       
        using var request = new HttpRequestMessage(HttpMethod.Post
                                                 , "api/conversation/converse/stream")
                            {
                                    Content = JsonContent.Create(requestDto)
                            };

        using var response = await _httpClient.SendAsync(request
                                                 , HttpCompletionOption.ResponseHeadersRead
                                                 , ct);

        response.EnsureSuccessStatusCode();

        Connectivity.ReportOnline();
        
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var       reader = new StreamReader(stream);

        while ( ! reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            
            if (string.IsNullOrWhiteSpace(line)) continue;
            if ( ! line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
            
            var payload = line.Substring("data:".Length);

            // server writes "data: {chunk}"
            if (payload.StartsWith(" ", StringComparison.Ordinal))
                payload = payload.Substring(1);

            // IMPORTANT: do NOT Trim() — it destroys legitimate spaces
            if (payload.Length > 0)
                yield return payload;

        }
    }

    public override async Task Ping()
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/health/ping");

            response.EnsureSuccessStatusCode();

            Connectivity.ReportOnline();
        }
        catch (Exception e)
        {
            Connectivity.ReportOffline(e);

        }
    }
}