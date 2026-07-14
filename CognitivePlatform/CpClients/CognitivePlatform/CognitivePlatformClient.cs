using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Core.Environment.Models;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

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

            if (response.IsSuccessStatusCode)
            {
                Connectivity.ReportOnline();

                return await response.Content.ReadFromJsonAsync<ConverseResponseDto>()
                    ?? new ConverseResponseDto { Message = "(empty response)" };
            }

            // Server responded with an error status — not a connectivity failure
            return new ConverseResponseDto { Message = await BuildServerErrorMessageAsync(response) };
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

    private static async Task<string> BuildServerErrorMessageAsync( HttpResponseMessage response )
    {
        var statusCode = (int)response.StatusCode;

        try
        {
            var errorDto = await response.Content.ReadFromJsonAsync<ConverseResponseDto>();

            if (errorDto?.Message.Contains("Rate limit reached", StringComparison.OrdinalIgnoreCase) ?? false)
                return MarkdownFormatter.Format(errorDto.Message);

            if (string.IsNullOrWhiteSpace(errorDto?.Message).Not())
                return $"Server error ({statusCode}): {errorDto!.Message}";
        }
        catch
        {
            // Content not JSON or not parseable — fall through to generic message
        }

        return $"Server error: the API returned HTTP {statusCode} {response.ReasonPhrase}.";
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


    private static readonly string[] DestructiveVerbs = ["delete", "remove", "clear", "destroy", "reset", "erase", "purge"];

    private static bool IsDestructiveInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var trimmed = input.Trim().ToLowerInvariant();
        foreach (var verb in DestructiveVerbs)
        {
            if (trimmed.StartsWith(verb)) return true;
        }
        return false;
    }

    private static ConverseRequestDto BuildRequest( string userMessage
                                                   , string conversationId
                                                   , string model )
    {
        var isFastPath    = FastPathIntentDetector.IsFastPathIntent(userMessage);
        var isDestructive = IsDestructiveInput(userMessage);

        return new ConverseRequestDto
               {
                       Input     = userMessage
                     , SessionId = conversationId
                     , Model     = model
                     , FastPath  = isFastPath
                     , Streaming = isFastPath.Not() && !isDestructive
               };
    }

    public override async IAsyncEnumerable<string> ConverseStreamAsync( string                                     userMessage
                                                                       , string                                     conversationId
                                                                       , string                                     model
                                                                       , [EnumeratorCancellation] CancellationToken ct = default )
    {
        var requestDto = BuildRequest(userMessage, conversationId, model);
        var route = "api/conversation/converse";
        route += requestDto.Streaming
                         ? "/stream"
                         : string.Empty;
        
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
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

        while (reader.EndOfStream.Not() 
            && ct.IsCancellationRequested.Not())
        {
            var line = await reader.ReadLineAsync(ct) ?? string.Empty;

            if (line.HasNoValue()) continue;

            if (line.TrimStart().StartsWith("{"))
            {
                var fullJson = line + await reader.ReadToEndAsync(ct);
                string? messageToYield = null;
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var converseResponse = System.Text.Json.JsonSerializer.Deserialize<ConverseResponseDto>(fullJson, options);
                    messageToYield = converseResponse?.Message;
                }
                catch
                {
                    // Ignore
                }

                if (messageToYield != null)
                {
                    yield return messageToYield;
                }
                yield break;
            }

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase).Not()) continue;

            var payload = line["data:".Length..];

            if (payload.StartsWith(' ')) payload = payload[1..];

            if (payload.Length > 0) yield return payload;
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

    public override async Task<List<ActionMetadataDto>> GetActionsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ActionMetadataDto>>("api/system/actions"
                                                                              , cancellationToken: ct)
                ?? new List<ActionMetadataDto>();
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning($"GetActionsAsync failed: {ex.Message}"
                                      , Category.CognitivePlatformClient);
            return new List<ActionMetadataDto>();
        }
    }

    public override string ToString()
        => $"{nameof(CognitivePlatformClient)} :: HttpClient -> {_httpClient.BaseAddress}";
}