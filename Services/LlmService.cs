using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Contracts;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.Services;

public class LlmService : ILlmService
{

    private readonly HttpClient            _httpClient;
    private readonly IPersonalityService   _personalityService;
    private readonly IMemoryService        _memoryService;
    private readonly IRoleInjectionService _roleInjection;
    private readonly ILoggingService       _loggingService;
    private readonly OllamaConfigService   _configService;

    private readonly IOptions<MemoryRetrievalOptions> _memOpts;
    private readonly int                              _timeoutSeconds;
    private readonly int                              _maxRetries;
    private readonly bool                             _enablePromptChunking;
    private readonly int                              _chunkSize;

    private OllamaConfig _config;

    public LlmService(IPersonalityService              personalityService
                    , HttpClient                       httpClient
                    , IMemoryService                   memoryService
                    , IRoleInjectionService            roleInjection
                    , ILoggingService                  loggingService
                    , OllamaConfigService              configService
                    , IOptions<MemoryRetrievalOptions> memOpts
                    , int                              timeoutSeconds       = 300
                    , int                              maxRetries           = 3
                    , bool                             enablePromptChunking = false
                    , int                              chunkSize            = 8000)
    {
        _personalityService   = personalityService;
        _memoryService        = memoryService;
        _roleInjection        = roleInjection;
        _loggingService       = loggingService;
        _configService        = configService;
        _memOpts              = memOpts;
        _timeoutSeconds       = timeoutSeconds;
        _maxRetries           = maxRetries;
        _enablePromptChunking = enablePromptChunking;
        _chunkSize            = chunkSize;

        _httpClient = httpClient;
        _config     = _configService.GetConfig();

        _configService.ConfigChanged += cfg => _config = cfg;
    }

    private static string BuildUrl(OllamaConfig config
                                 , string       relativePath)
    {
        var host = config.Host?.Trim() ?? StringConsts.OllamaServerUrl;
        
        if (!host.EndsWith("/")) host += "/";
        
        return host + relativePath.TrimStart('/');
    }

    public async Task<bool> CheckApiHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUrl(_config
                                                             , ""));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    public IAsyncEnumerable<string> SendPromptStreamingAsync(string                                     prompt
                                                           , [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var req = new LlmRequest
                  {
                      UserPrompt   = prompt,
                      SystemPrompt = _personalityService.Current?.SystemPrompt ?? "You are a helpful AI.",
                      Personality  = _personalityService.Current,
                      OllamaConfig = _personalityService.Current?.OllamConfiguration
                  };

        return SendPromptStreamingAsync(req, cancellationToken);
    }

    private async IAsyncEnumerable<string> ReadStreamAsync(HttpResponseMessage response)
    {
        // Make a channel to buffer chunks
        var channel = Channel.CreateUnbounded<string>();

        // Start the background task to read from the stream and write to the channel
        _ = Task.Run(async () =>
        {
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var       reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Remove the "data: " prefix if present
                    if (line.StartsWith("data:"))
                    {
                        line = line.Substring("data:".Length).Trim();
                    }

                    // Check for the "[DONE]" signal and break the loop
                    if (line == "[DONE]")
                    {
                        break;
                    }

                    // Deserialize the JSON chunk and extract the content
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(line);
                        if (jsonDoc.RootElement.TryGetProperty("message"
                                                             , out var messageElement)
                         && messageElement.TryGetProperty("content"
                                                        , out var contentElement))
                        {
                            var content = contentElement.GetString();
                            if (!string.IsNullOrEmpty(content))
                            {
                                await channel.Writer.WriteAsync(content);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _loggingService.LogError(ex
                                               , $"Failed to parse JSON line: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Propagate any exceptions to the consuming code
                channel.Writer.TryComplete(ex);
            }
            finally
            {
                // Always complete the channel writer when done
                channel.Writer.Complete();
            }
        });

        // Yield values from the channel
        await foreach (var chunk in channel.Reader.ReadAllAsync())
        {
            yield return chunk;
        }
    }

    private static bool IsSocketClose(SocketException ex) =>
        ex.SocketErrorCode is SocketError.ConnectionReset or SocketError.Shutdown;

    private IEnumerable<string> ChunkPrompt(string prompt)
    {
        for (var i = 0; i < prompt.Length; i += _chunkSize)
        {
            yield return prompt.Substring(i, Math.Min(_chunkSize
                                                    , prompt.Length - i));
        }
    }
    
    public async Task<string> SendPromptStreamingAsync(Personality personality, string message)
    {
        ArgumentNullException.ThrowIfNull(personality);
        if (message.HasNoValue()) return string.Empty;

        // Build request that temporarily sets current persona for request only
        var req = new LlmRequest
                  {
                      UserPrompt   = message,
                      Personality  = personality,
                      SystemPrompt = personality.SystemPrompt,
                      OllamaConfig = personality.OllamConfiguration
                  };

        var sb = new StringBuilder();
        await foreach (var chunk in SendPromptStreamingAsync(req).ConfigureAwait(false))
        {
            sb.Append(chunk);
        }

        return sb.ToString();
    }

    // public async Task<string> SendPromptStreamingAsync(Personality personality
    //                                                  , string      message)
    // {
    //     ArgumentNullException.ThrowIfNull(personality);
    //     
    //     if (message.HasNoValue()) return string.Empty;
    //
    //     // Temporarily switch to the given personality
    //     var previous = _personalityService.Current;
    //     _personalityService.SetCurrent(personality.Name);
    //
    //     try
    //     {
    //         var sb = new StringBuilder();
    //         await foreach (var chunk in SendPromptStreamingAsync(message))
    //         {
    //             sb.Append(chunk);
    //         }
    //
    //         return sb.ToString();
    //     }
    //     finally
    //     {
    //         // Restore previous personality
    //         if (previous != null!)
    //             _personalityService.SetCurrent(previous.Name);
    //     }
    // }
    
    //New
    public async IAsyncEnumerable<string> SendPromptStreamingAsync(LlmRequest request
                                                                 , [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Build final prompt using request (system/context/user) and/or personality
        var finalPrompt = await BuildFinalPromptAsync(request, cancellationToken);
        
        // Determine which config to use (request override vs. service-level config)
        var cfg = request.OllamaConfig ?? _configService.Current;
        
        // Stream from Ollama
        await foreach (var chunk in StreamFromOllamaAsync(finalPrompt, cfg, cancellationToken))
            yield return chunk;
        
    }


    private async IAsyncEnumerable<string> StreamFromOllamaAsync(string                   finalPrompt
                                                               , OllamaConfig             config
                                                               , [EnumeratorCancellation] 
                                                                 CancellationToken        cancellation = default)
    {
        _loggingService.LogInformation($"[LlmService] Sending prompt to Ollama (model={config.Model}):\n{Truncate(finalPrompt, 500)}");

        // Example: using HttpClient to call Ollama's streaming endpoint
        using var request = new HttpRequestMessage(HttpMethod.Post
                                                 , GetGenerateEndpoint(config))
                            {
                                Content = JsonContent.Create(new
                                                             {
                                                                 model       = config.Model
                                                               , prompt      = finalPrompt
                                                               , stream      = true
                                                               , temperature = config.Temperature
                                                               , num_predict = config.NumPredict
                                                                 // add any other Ollama parameters you support
                                                             })
                            };

        using var response = await _httpClient.SendAsync(request
                                                       , HttpCompletionOption.ResponseHeadersRead
                                                       , cancellation);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellation);
        using var reader = new StreamReader(stream);

        while (reader.EndOfStream.Not() 
            && cancellation.IsCancellationRequested.Not())
        {
            var line = await reader.ReadLineAsync();
            
            if (line.HasNoValue()) continue;

            // Ollama streams JSON objects, one per line
            var json = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            if (json?.Response is not null)
            {
                yield return json.Response;
            }

            if (json?.Done == true)
                yield break;
        }
    }
    
    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) ? value : value[..Math.Min(value.Length, maxLength)];
    
    private static string GetGenerateEndpoint(OllamaConfig cfg)
    {
        return $"{cfg.Host?.TrimEnd('/') ?? StringConsts.OllamaServerUrl}/api/generate";
        
        // return $"{BuildUrl(cfg, StringConsts.OllamaChatEndpoint)}/api/generate";
    }

    // A model for the streaming response
    private sealed class OllamaStreamChunk
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
    
    private async Task<string> BuildFinalPromptAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        // Prefer personality fields from request if present, otherwise fallback to personality service
        var persona      = request.Personality ?? _personalityService.Current;
        var personaName  = persona?.Name ?? "Default";
        var systemPrompt = request.SystemPrompt ?? persona?.SystemPrompt ?? "You are a helpful assistant.";

        // Memory context (prefer request.Context if provided)
        string? memorySummary;
        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            memorySummary = request.Context;
        }
        else
        {
            var memCtx = await _memoryService.GetContextForTurnAsync(request.UserPrompt, _memOpts, cancellationToken).ConfigureAwait(false);
            memorySummary = memCtx?.Summary;
        }

        var injectedSystemPrompt = _roleInjection.BuildInjectedSystemPrompt(systemPrompt, personaName, memorySummary);

        var userPrompt = request.UserPrompt ?? string.Empty;

        if (!_enablePromptChunking || userPrompt.Length <= _chunkSize)
        {
            return $@"System: {injectedSystemPrompt}
Conversation so far:
User: {userPrompt}
AI:";
        }

        var chunks = ChunkPrompt(userPrompt);
        return $@"System: {injectedSystemPrompt}
Conversation so far:
User: {string.Join("\n", chunks)}
AI:";
    }
}
