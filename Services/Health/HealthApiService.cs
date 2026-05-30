using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Services.Health.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.Services.Health;

/// <summary>
/// Exposes /health/* HTTP endpoints on the LAN so the CP API can pull Health Connect data.
/// Runs only on Android; exits immediately on other platforms.
/// </summary>
public sealed class HealthApiService : BackgroundService
{
    private readonly HealthGatewayConfig    _config;
    private readonly ILogger<HealthApiService> _logger;
    private readonly IHealthConnectManager?  _healthConnect;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HealthApiService(IOptions<HealthGatewayConfig>  config
                          , ILogger<HealthApiService>       logger
                          , IServiceProvider                services)
    {
        _config        = config.Value;
        _logger        = logger;
        _healthConnect = services.GetService<IHealthConnectManager>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsAndroid())
        {
            _logger.LogInformation("Health API gateway: skipped on non-Android platform");
            return;
        }

        if (_healthConnect is null)
        {
            _logger.LogWarning("Health API gateway: IHealthConnectManager not registered; gateway disabled");
            return;
        }

        var prefix   = $"http://+:{_config.Port}/health/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);

        try
        {
            listener.Start();
            _logger.LogInformation("Health API gateway listening on port {Port}", _config.Port);

            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = Task.Run(() => HandleRequestAsync(context, stoppingToken), stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Health API gateway crashed");
        }
        finally
        {
            if (listener.IsListening)
                listener.Stop();

            _logger.LogInformation("Health API gateway stopped");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request  = context.Request;
        var response = context.Response;

        try
        {
            if (!ValidateKey(request))
            {
                await WriteErrorAsync(response, 403, "Forbidden");
                return;
            }

            var path = request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;

            switch (path)
            {
                case "/health/ping":
                    await WriteJsonAsync(response, new { status = "ok" }, ct);
                    break;

                case "/health/steps":
                    await HandleMetricEndpointAsync(response
                                                  , request
                                                  , (from, to) => _healthConnect!.GetStepCountAsync(from, to, ct)
                                                  , ct);
                    break;

                case "/health/sleep":
                    await HandleMetricEndpointAsync(response
                                                  , request
                                                  , (from, to) => _healthConnect!.GetSleepAsync(from, to, ct)
                                                  , ct);
                    break;

                case "/health/heart-rate":
                    await HandleMetricEndpointAsync(response
                                                  , request
                                                  , (from, to) => _healthConnect!.GetHeartRateAsync(from, to, ct)
                                                  , ct);
                    break;

                case "/health/distance":
                    await HandleMetricEndpointAsync(response
                                                  , request
                                                  , (from, to) => _healthConnect!.GetDistanceAsync(from, to, ct)
                                                  , ct);
                    break;

                default:
                    await WriteErrorAsync(response, 404, "Not Found");
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error handling Health API request for {Path}", request.Url?.AbsolutePath);
            try { await WriteErrorAsync(response, 500, "Internal Server Error"); }
            catch { /* best effort on error path */ }
        }
    }

    private async Task HandleMetricEndpointAsync<T>(HttpListenerResponse                         response
                                                  , HttpListenerRequest                          request
                                                  , Func<DateTimeOffset, DateTimeOffset, Task<T>> fetch
                                                  , CancellationToken                            ct)
    {
        var query = request.QueryString;
        var styles = DateTimeStyles.RoundtripKind;

        if (!DateTimeOffset.TryParse(query["from"], null, styles, out var from)
         || !DateTimeOffset.TryParse(query["to"],   null, styles, out var to))
        {
            await WriteErrorAsync(response, 400, "Missing or invalid 'from' / 'to' query parameters (ISO 8601 required)");
            return;
        }

        var result = await fetch(from, to);
        await WriteJsonAsync(response, result, ct);
    }

    private bool ValidateKey(HttpListenerRequest request)
    {
        if (string.IsNullOrEmpty(_config.SharedSecret))
            return true;

        var key = request.Headers["X-CP-Key"];
        return key == _config.SharedSecret;
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, object payload, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));

        response.StatusCode              = 200;
        response.ContentType             = "application/json; charset=utf-8";
        response.ContentLength64         = bytes.Length;
        response.Headers["Cache-Control"] = "no-store";

        await response.OutputStream.WriteAsync(bytes, ct);
        response.Close();
    }

    private static async Task WriteErrorAsync(HttpListenerResponse response, int statusCode, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = message }));

        response.StatusCode      = statusCode;
        response.ContentType     = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;

        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }
}
