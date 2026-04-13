using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Core.Environment.Models;
using LocalAIAssistant.Services.Logging;
using static CP.Client.Core.Intent.FastPathIntentDetector;

namespace LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;

public class CognitivePlatformClient : CognitivePlatformClientBase
{
    private readonly HttpClient      _httpClient;
    private readonly ILoggingService _loggingService;

    public IConnectivityReporter ConnectivityReporter { get; private set; }

    public CognitivePlatformClient( HttpClient            httpClient
                                  , IConnectivityReporter connectivity
                                  , ILoggingService        loggingService )
    {
        _httpClient       = httpClient;
        Connectivity      = connectivity;
        ConnectivityReporter = connectivity;
        _loggingService   = loggingService;
    }

    public override async Task<ConverseResponseDto> ConverseAsync( string userMessage
                                                                 , string conversationId
                                                                 , string model )
    {
        var response = new HttpResponseMessage();

        try
        {
            if (Connectivity.Online().Not()) throw new TaskCanceledException("API Offline");

            var request = BuildRequest(userMessage, conversationId, model);

            response = await _httpClient.PostAsJsonAsync("api/conversation/converse", request);

            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                Connectivity.ReportOnline();

                return await response.Content.ReadFromJsonAsync<ConverseResponseDto>()
                    ?? new ConverseResponseDto { Message = "(empty response)" };
            }
        }
        catch (HttpRequestException ex)
        {
            Connectivity.ReportOffline(ex);
        }
        catch (TaskCanceledException ex)
        {
            Connectivity.ReportOffline(ex);
        }

        var errorResponse   = response.Content.ReadFromJsonAsync<ConverseResponseDto>();
        if (errorResponse.Result?.Message.Contains("Rate limit reached", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var formattedMessage = MarkdownFormatter.Format(errorResponse.Result.Message);
            return new ConverseResponseDto { Message = formattedMessage };
        }
        
        var shortenTextBy   = userMessage.Length < 25 ? userMessage.Length : 25;
        var responseMessage = $"Added to queued:{Environment.NewLine}{userMessage[..shortenTextBy]}...{Environment.NewLine}{conversationId}";

        Connectivity.ReportOffline(responseMessage);

        return new ConverseResponseDto { Message = responseMessage };
    }

    public static class MarkdownFormatter
    {
        public static string Format( string raw )
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            var result = raw;

            // 1. Normalize escaped newlines
            result = result.Replace("\\r\\n"
                                  , "\n")
                           .Replace("\\n"
                                  , "\n")
                           .Replace("\r\n"
                                  , "\n");

            // 2. Ensure code fences are on their own lines
            result = NormalizeCodeFences(result);

            // 3. Optional: trim excessive blank lines
            result = CollapseExtraNewlines(result);

            return result.Trim();
        }

        private static string NormalizeCodeFences( string input )
        {
            var lines  = input.Split('\n');
            var output = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("```"))
                {
                    // Force fence to be isolated
                    output.Add(trimmed);
                }
                else if (trimmed.EndsWith("```") && trimmed.Length > 3)
                {
                    // Handle inline closing fence
                    var content = trimmed[..^3].TrimEnd();
                    if (!string.IsNullOrEmpty(content))
                        output.Add(content);

                    output.Add("```");
                }
                else
                {
                    output.Add(line);
                }
            }

            return string.Join("\n"
                             , output);
        }

        private static string CollapseExtraNewlines( string input )
        {
            var lines  = input.Split('\n');
            var output = new List<string>();

            bool lastWasEmpty = false;

            foreach (var line in lines)
            {
                var isEmpty = string.IsNullOrWhiteSpace(line);

                if (isEmpty)
                {
                    if (!lastWasEmpty)
                        output.Add(string.Empty);
                }
                else
                {
                    output.Add(line);
                }

                lastWasEmpty = isEmpty;
            }

            return string.Join("\n"
                             , output);
        }
    }


    private static ConverseRequestDto BuildRequest( string userMessage
                                                  , string conversationId
                                                  , string model )
    {
        var isFastPath = IsFastPathIntent(userMessage);

        return new ConverseRequestDto
               {
                       Input     = userMessage
                     , SessionId = conversationId
                     , Model     = model
                     , FastPath  = isFastPath
                     , Streaming = isFastPath.Not()
               };
    }

    public override async IAsyncEnumerable<string> ConverseStreamAsync( string                                     userMessage
                                                                       , string                                     conversationId
                                                                       , string                                     model
                                                                       , [EnumeratorCancellation] CancellationToken ct = default )
    {
        var requestDto = BuildRequest(userMessage, conversationId, model);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/conversation/converse/stream")
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

        while (reader.EndOfStream.Not() && ct.IsCancellationRequested.Not())
        {
            var line = await reader.ReadLineAsync(ct) ?? string.Empty;

            if (line.HasNoValue()) continue;
            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase).Not()) continue;

            var payload = line["data:".Length..];

            if (payload.StartsWith(' ')) payload = payload[1..];

            if (payload.Length > 0)
                yield return payload;
        }
    }

    public override async Task<SystemEnvironmentInfo> SystemEnvironmentAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<SystemEnvironmentInfo>("system/environment"
                                                                       , cancellationToken: ct)
            ?? new SystemEnvironmentInfo();
    }

    public override async Task<HttpResponseMessage> Ping( string callersCaller
                                                        , [CallerFilePath]   string caller = ""
                                                        , [CallerMemberName] string member = "" )
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
        catch (Exception ex)
        {
            _loggingService.LogWarning($"Ping ({endpoint}) failed: {ex.Message}"
                                     , Category.CognitivePlatformClient);
            Connectivity.ReportOffline(ex);

            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

    public override async Task<GroqUsageDto> GetUsageAsync(CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<GroqUsageDto>("api/system/usage"
                                                                   , cancellationToken: ct)
                ?? new GroqUsageDto();
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning($"GetUsageAsync failed: {ex.Message}"
                                      , Category.CognitivePlatformClient);
            return new GroqUsageDto();
        }
    }

    public override string ToString()
        => $"{nameof(CognitivePlatformClient)} :: HttpClient -> {_httpClient.BaseAddress}";
}