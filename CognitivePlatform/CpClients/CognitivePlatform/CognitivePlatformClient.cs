using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Core.Environment.Models;
using LocalAIAssistant.Services.Logging;
using static CP.Client.Core.Intent.FastPathIntentDetector;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

public class CognitivePlatformClient : ICognitivePlatformClient
{
    private readonly HttpClient       _httpClient;
    private readonly ILoggingService  _loggingService;

    public IConnectivityReporter Connectivity { get; private set; }

    public CognitivePlatformClient( HttpClient               httpClient
                                  , IConnectivityReporter    connectivity
                                  , ILoggingService          loggingService)
    {
        _httpClient       = httpClient;
        Connectivity      = connectivity;
        _loggingService   = loggingService;
    }

    public override async Task<ConverseResponseDto> ConverseAsync(string userMessage
                                                                , string conversationId
                                                                , string model)
    {
        var response = new HttpResponseMessage();
        
        try
        {
            if (Connectivity.Online().Not()) throw new TaskCanceledException("API Offline");
            
            var request = BuildRequest(userMessage
                                     , conversationId
                                     , model);

            response = await _httpClient.PostAsJsonAsync("api/conversation/converse", request);

            if (response.IsSuccessStatusCode)
            {
                Connectivity.ReportOnline();
            
                return await response.Content.ReadFromJsonAsync<ConverseResponseDto>()
                    ?? new ConverseResponseDto { Message = "(empty response)" };    
            }
        }
        catch (HttpRequestException ex)
        {
            // Probably a timeout
            Connectivity.ReportOffline(ex);
        }
        catch (TaskCanceledException ex)
        {
            Connectivity.ReportOffline(ex);
        }
        
        // API is Offline
        var shortenTextBy = userMessage.Length < 25
                                    ? userMessage.Length
                                    : 25;
        var responseMessage = $"Added to queued:{Environment.NewLine}{userMessage[..shortenTextBy]}...{Environment.NewLine}{conversationId}";
            
        Connectivity.ReportOffline(responseMessage);
            
        return new ConverseResponseDto { Message = responseMessage };
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

    public override async IAsyncEnumerable<string> ConverseStreamAsync (string                                     userMessage
                                                                      , string                                     conversationId
                                                                      , string                                     model
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

    public override async Task<SystemEnvironmentInfo> SystemEnvironmentAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<SystemEnvironmentInfo>("system/environment"
                                                                       , cancellationToken: ct) ?? new SystemEnvironmentInfo();
    }
    

    public override async Task Ping()
    {
        var endpoint = $"health/ready";
        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();

            Connectivity.ReportOnline();
        }
        catch (Exception e)
        {
            _loggingService.LogWarning($"Ping ({endpoint}) failed: {e.Message}", Category.CognitivePlatformClient);
            Connectivity.ReportOffline(e);
        }
    }

    public override async Task<HttpResponseMessage> Ready()
    {
        return await _httpClient.GetAsync("health/ready");
    }
    
    public override string ToString()
    {
        return $"{nameof(CognitivePlatformClient)} :: HttpClient -> {_httpClient.BaseAddress}";
    }
}