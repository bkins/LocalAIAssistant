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

        while ( reader.EndOfStream.Not() 
             && ct.IsCancellationRequested.Not())
        {
            var line = await reader.ReadLineAsync(ct) ?? string.Empty;
            
            if (line.HasNoValue()) continue;
            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase).Not()) continue;
            
            var payload = line["data:".Length..];

            // server writes "data: {chunk}"
            if (payload.StartsWith(' ')) payload = payload[1..];

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
    
    public override async Task<HttpResponseMessage> Ping(string callersCaller
                                                       , [CallerFilePath] string caller = ""
                                                       , [CallerMemberName] string member = "")
    {
        var fileName = Path.GetFileName(caller);
        var source   = $"{callersCaller}->{member}";
        var endpoint = $"health/ready?caller={source}";
        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();
            
            Connectivity.ReportOnline();

            return response;

        }
        catch (Exception e)
        {
            _loggingService.LogWarning($"Ping ({endpoint}) failed: {e.Message}", Category.CognitivePlatformClient);
            Connectivity.ReportOffline(e);
            
            return new HttpResponseMessage();
        }
    }
    
    public override string ToString()
    {
        return $"{nameof(CognitivePlatformClient)} :: HttpClient -> {_httpClient.BaseAddress}";
    }
}