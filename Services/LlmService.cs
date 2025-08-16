using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Services.AiMemory;
using LocalAIAssistant.Services.AiMemory.Interfaces;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Services;

public class LlmService : ILlmService
{

    private readonly HttpClient             _httpClient;
    private readonly IPersonalityService    _personalityService;
    private readonly IMemoryService         _memoryService;
    private readonly IRoleInjectionService  _roleInjection;
    private readonly MemoryRetrievalOptions _memOpts = new();
    private readonly int                    _timeoutSeconds;
    private readonly int                    _maxRetries;
    private readonly bool                   _enablePromptChunking;
    private readonly int                    _chunkSize;

    // private const    string     BaseUrl = "http://10.0.2.2:11434/api/chat"; // Android emulator magic localhost
    //private const    string     BaseUrl = "http://192.168.0.33:11434/api/chat"; // Android emulator magic localhost
    //192.168.0.33
    private readonly string _baseUrl = ApiConfig.OllamaBaseUrl + "api/chat";

    public LlmService(IPersonalityService   personalityService
                    , HttpClient            httpClient
                    , IMemoryService        memoryService
                    , IRoleInjectionService roleInjection
                    , int                   timeoutSeconds       = 300
                    , int                   maxRetries           = 3
                    , bool                  enablePromptChunking = false
                    , int                   chunkSize            = 8000)
    {
        _personalityService   = personalityService;
        _memoryService        = memoryService;
        _roleInjection        = roleInjection;
        _timeoutSeconds       = timeoutSeconds;
        _maxRetries           = maxRetries;
        _enablePromptChunking = enablePromptChunking;
        _chunkSize            = chunkSize;

        _httpClient = new HttpClient
                      {
                          Timeout = TimeSpan.FromSeconds(_timeoutSeconds)
                      };
    }

    public async Task<bool> CheckApiHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiConfig.OllamaBaseUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async IAsyncEnumerable<string> SendPromptStreamingAsync(string prompt)
    {
        HttpResponseMessage response;
        try
        {
            response = await Task.Run(() => SendPromptRequestAsync(prompt));
        }
        catch (Exception ex)
        {
            var s = $"[Error] {ex.Message}";
            response = new HttpResponseMessage
                       {
                           Content = new StringContent(s
                                                     , Encoding.UTF8
                                                     , "application/json")
                         , StatusCode   = HttpStatusCode.ExpectationFailed
                         , ReasonPhrase = ex.ToString()
                       };
            //yield return $"[Error] Request failed: {ex.Message}";
            // yield break;
        }

        if (response == null)
        {
            yield return "[Error] No response.";
            yield break;
        }

        if (response.IsSuccessStatusCode.Not())
        {
            yield return $"[Error] {response.StatusCode}:  {response.ReasonPhrase}";
            yield break;
        }

        await foreach (var chunk in ReadChunksAsyncSafe(response))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<string> ReadChunksAsyncSafe(HttpResponseMessage response)
    {
        var chunkEnumerable = await Task.Run(() => ReadChunksAsync(response));
        await foreach (var chunk in chunkEnumerable)
        {
            yield return chunk;
        }
    }

    private async Task<HttpResponseMessage> SendPromptRequestAsync(string            prompt
                                                                 , CancellationToken cancellationToken = default)
    {
        _personalityService.SetCurrent("Roleplay");
        var systemPrompt = _personalityService.Current.SystemPrompt;
        var personaName = _personalityService.Current.Name;

        var ctx = await _memoryService.GetContextForTurnAsync(prompt
                                                            , _memOpts);
        var injectedSystemPrompt = _roleInjection.BuildInjectedSystemPrompt(systemPrompt
                                                                          , personaName
                                                                          , ctx.Summary);

        // Apply chunking if enabled
        var finalPrompt = prompt;
        if (_enablePromptChunking && prompt.Length > _chunkSize)
        {
            var chunks = ChunkPrompt(prompt);
            finalPrompt = string.Join("\n"
                                    , chunks);
        }

        var payload = new
                      {
                          model = "nollama/mythomax-l2-13b:Q5_K_S", messages = new[]
                                                                               {
                                                                                   new { role = "system", content = injectedSystemPrompt }
                                                                                 , new { role = "user", content   = finalPrompt }
                                                                               }
                        , stream = true
                      };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json
                                      , Encoding.UTF8
                                      , "application/json");

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post
                                                   , _baseUrl)
                              {
                                  Content = content
                              };

                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var response = await _httpClient.SendAsync(
                    request
                  , HttpCompletionOption.ResponseHeadersRead
                  , cancellationTokenSource.Token
                ).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx && IsSocketClose(socketEx))
            {
                if (attempt == _maxRetries) throw;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2
                                                             , attempt))
                               , cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == _maxRetries)
                    throw new TimeoutException("Streaming request timed out."
                                             , ex);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2
                                                             , attempt))
                               , cancellationToken);
            }
        }

        throw new Exception("Failed to send streaming prompt after retries.");
    }
    // private async Task<HttpResponseMessage> SendPromptRequestAsync(string prompt)
    // {
    //     _personalityService.SetCurrent("Roleplay");
    //     var systemPrompt =  _personalityService.Current.SystemPrompt;
    //     var personaName  = _personalityService.Current.Name;
    //     
    //     var ctx = await _memoryService.GetContextForTurnAsync(prompt, _memOpts);
    //     var injectedSystemPrompt = _roleInjection.BuildInjectedSystemPrompt(systemPrompt, personaName, ctx.Summary);
    //
    //     var payload = new
    //                   {
    //                       model = "nollama/mythomax-l2-13b:Q5_K_S" //"mistral-openorca"
    //                     , messages = new[]
    //                                  {
    //                                      new { role = "system", content = injectedSystemPrompt }
    //                                    , new { role = "user", content   = prompt }
    //                                  }
    //                     , stream = true
    //                   };
    //
    //     var json = JsonSerializer.Serialize(payload);
    //     var content = new StringContent(json, Encoding.UTF8, "application/json");
    //
    //     // Offload to background thread to avoid NetworkOnMainThreadException
    //     return await Task.Run(async () =>
    //     {
    //         
    //          var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
    //                        {
    //                            Content = content
    //                        };
    //         
    //          var response = await _httpClient.SendAsync(request,
    //                                                     HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
    //         
    //         response.EnsureSuccessStatusCode();
    //         return response;
    //     });
    // }


    private async IAsyncEnumerable<string> ReadChunksAsync(HttpResponseMessage response)
    {
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(line))
                continue;
            var chunk = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message"
                                                 , out var messageElement)
                 && messageElement.TryGetProperty("content"
                                                , out var contentElement))
                {
                    chunk = contentElement.GetString();
                    if (string.IsNullOrEmpty(chunk))
                    {
                        continue;
                        //yield return chunk;
                    }
                }
            }
            catch (JsonException ex)
            {
                var s = $"[Error] {ex.Message}";
                //yield return $"[JSON Error] {ex.Message}";
            }

            yield return chunk ?? string.Empty;
        }
    }

    public async Task<string> SendPromptAsync(string            model
                                            , string            prompt
                                            , CancellationToken cancellationToken = default)
    {
        if (_enablePromptChunking && prompt.Length > _chunkSize)
        {
            var chunks = ChunkPrompt(prompt);
            var sb = new StringBuilder();
            foreach (var chunk in chunks)
            {
                var result = await SendSinglePromptAsync(model
                                                       , chunk
                                                       , cancellationToken);
                sb.Append(result);
            }

            return sb.ToString();
        }
        else
        {
            return await SendSinglePromptAsync(model
                                             , prompt
                                             , cancellationToken);
        }
    }

    private async Task<string> SendSinglePromptAsync(string            model
                                                   , string            prompt
                                                   , CancellationToken cancellationToken)
    {
        var payload = new
                      {
                          model = model, prompt = prompt, stream = false
                      };

        var content = new StringContent(JsonSerializer.Serialize(payload)
                                      , Encoding.UTF8
                                      , "application/json");

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                using var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate"
                                                               , content
                                                               , cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<OllamaResponse>(json);
                return result?.Response ?? string.Empty;
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx && IsSocketClose(socketEx))
            {
                if (attempt == _maxRetries) throw;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2
                                                             , attempt))
                               , cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout handling
                if (attempt == _maxRetries)
                    throw new TimeoutException("Ollama request timed out."
                                             , ex);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2
                                                             , attempt))
                               , cancellationToken);
            }
        }

        return string.Empty;
    }

    private bool IsSocketClose(SocketException ex)
    {
        return ex.SocketErrorCode == SocketError.ConnectionReset || ex.SocketErrorCode == SocketError.Shutdown;
    }

    private IEnumerable<string> ChunkPrompt(string prompt)
    {
        for (int i = 0; i < prompt.Length; i += _chunkSize)
        {
            yield return prompt.Substring(i
                                        , Math.Min(_chunkSize
                                                 , prompt.Length - i));
        }
    }

    private class OllamaResponse
    {

        public string Response { get; set; }

    }

}