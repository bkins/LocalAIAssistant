using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalAIAssistant.Services.Health;

/// <summary>
/// Posts unhandled exception reports from LAA to the CP API's
/// <c>POST /diagnostics/client-crash</c> endpoint, and writes a local fallback
/// to <see cref="FileSystem.AppDataDirectory"/>/crash-log.jsonl when the API
/// is unreachable.
/// </summary>
public sealed class CrashReportService
{
    private readonly IHttpClientFactory              _http;
    private readonly ILogger<CrashReportService>     _logger;
    private readonly string                          _apiBaseUrl;
    private readonly string                          _localLogPath;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CrashReportService(IHttpClientFactory http, ILogger<CrashReportService> logger)
    {
        _http         = http;
        _logger       = logger;
        _apiBaseUrl   = BuildEnvironment.ApiBaseUrl.TrimEnd('/');
        _localLogPath = Path.Combine(FileSystem.AppDataDirectory, "crash-log.jsonl");
    }

    /// <summary>Attempts to report a crash both to the API and locally.</summary>
    public async Task ReportAsync(string message, string stackTrace, string source = "")
    {
        var payload = new
        {
            Platform   = "Android",
            Message    = message,
            StackTrace = stackTrace,
            Source     = source,
            Timestamp  = DateTime.UtcNow
        };

        // Always write locally first so we never lose a crash report.
        WriteLocal(payload);

        try
        {
            using var client = _http.CreateClient();
            client.Timeout   = TimeSpan.FromSeconds(5);

            var response = await client.PostAsJsonAsync($"{_apiBaseUrl}/diagnostics/client-crash"
                                                      , payload
                                                      , JsonOptions);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Crash report HTTP returned {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not POST crash report to API — local log written");
        }
    }

    private void WriteLocal(object payload)
    {
        try
        {
            var line = JsonSerializer.Serialize(payload, JsonOptions) + Environment.NewLine;
            File.AppendAllText(_localLogPath, line);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not write crash to local log at {Path}", _localLogPath);
        }
    }
}
