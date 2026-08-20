using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalAIAssistant.Services.Health;

/// <summary>
/// Background service that pushes today's health metrics from Health Connect
/// to the CP API every <see cref="PushIntervalMinutes"/> minutes.
/// This replaces the defunct <c>HealthApiService</c> (HttpListener pull model)
/// with a simpler push model that works on non-rooted Android devices.
/// </summary>
public sealed class HealthPushService : BackgroundService
{
    private const int PushIntervalMinutes = 5;

    private readonly IHealthConnectManager?          _healthConnect;
    private readonly IHttpClientFactory              _http;
    private readonly ILogger<HealthPushService>      _logger;
    private readonly string                          _apiBaseUrl;
    private readonly HealthGatewayConfig?            _gatewayConfig;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HealthPushService( IServiceProvider                  services
                            , IHttpClientFactory                 http
                            , ILogger<HealthPushService>         logger )
    {
        _healthConnect = services.GetService<IHealthConnectManager>();
        _http          = http;
        _logger        = logger;
        _apiBaseUrl    = BuildEnvironment.ApiBaseUrl.TrimEnd('/');

        var options = services.GetService<Microsoft.Extensions.Options.IOptions<HealthGatewayConfig>>();
        _gatewayConfig = options?.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Offload execution to a thread pool thread immediately to prevent blocking/deadlocking
        // the MAUI UI startup thread when accessing Health Connect API.
        await Task.CompletedTask.ConfigureAwait(false);
        await Task.Run(async () =>
        {
            if (!OperatingSystem.IsAndroid())
            {
                _logger.LogInformation("HealthPushService: skipped on non-Android platform");
                return;
            }

            if (_healthConnect is null)
            {
                _logger.LogWarning("HealthPushService: IHealthConnectManager not registered; push disabled");
                return;
            }

            _logger.LogInformation("HealthPushService starting — will push health data every {Interval} minutes to {Url}"
                                 , PushIntervalMinutes
                                 , _apiBaseUrl);

            // Push immediately on startup, then on a repeating timer.
            await PushTodayAsync(stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(PushIntervalMinutes));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
                    await PushTodayAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HealthPushService: unexpected error in push loop");
                }
            }

            _logger.LogInformation("HealthPushService stopped");
        }, stoppingToken).ConfigureAwait(false);
    }

    // Called by AppShell on resume so data is fresh when the user opens LAA.
    public Task<PushResult> PushNowAsync(CancellationToken ct = default)
        => Task.Run(() => PushTodayAsync(ct), ct);

    private async Task<PushResult> PushTodayAsync(CancellationToken ct)
    {
        try
        {
            var hasPermissions = await _healthConnect!.CheckPermissionsAsync(ct).ConfigureAwait(false);
            if (!hasPermissions)
            {
                var msg = "Health Connect permissions NOT granted — push skipped. Open Health Connect and grant permissions to the CP app.";
                _logger.LogWarning("HealthPushService: " + msg);
                return new PushResult(false, msg);
            }

            _logger.LogInformation("HealthPushService: Health Connect permissions confirmed — collecting data");

            var today    = DateTimeOffset.Now;
            var from     = new DateTimeOffset(today.Date, today.Offset);
            var to       = from.AddDays(1).AddTicks(-1);

            var steps     = await _healthConnect.GetStepCountAsync(from, to, ct).ConfigureAwait(false);
            var distance  = await _healthConnect.GetDistanceAsync(from, to, ct).ConfigureAwait(false);
            var heartRate = await _healthConnect.GetHeartRateAsync(from, to, ct).ConfigureAwait(false);
            var sleep     = await _healthConnect.GetSleepAsync(from, to, ct).ConfigureAwait(false);

            var snapshot = new
            {
                Date             = DateOnly.FromDateTime(today.Date),
                Steps            = steps.Steps,
                DistanceMetres   = distance.Metres,
                AverageHeartRate = heartRate.AverageBpm,
                MinHeartRate     = heartRate.MinBpm ?? 0,
                MaxHeartRate     = heartRate.MaxBpm ?? 0,
                SleepMinutes     = sleep.TotalMinutes,
                SleepSessions    = sleep.Sessions,
                Platform         = "Android"
            };

            _logger.LogInformation("HealthPushService: pushing snapshot — steps={Steps}, distance={Dist:F0}m, hr={Hr}bpm, sleep={Sleep}min"
                                 , snapshot.Steps
                                 , snapshot.DistanceMetres
                                 , snapshot.AverageHeartRate
                                 , snapshot.SleepMinutes);

            using var client  = _http.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl + "/");
            client.Timeout     = TimeSpan.FromSeconds(10);

            if ((_gatewayConfig?.SharedSecret).HasValue())
            {
                client.DefaultRequestHeaders.Add("X-CP-Key", _gatewayConfig.SharedSecret);
            }

            var response = await client.PostAsJsonAsync("health/data", snapshot, JsonOptions, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var errorMsg = $"API returned {response.StatusCode}: {body}";
                _logger.LogWarning("HealthPushService: " + errorMsg);
                return new PushResult(false, errorMsg);
            }
            else
            {
                _logger.LogInformation("HealthPushService: snapshot accepted by API");
                return new PushResult(true, "Snapshot pushed successfully");
            }
        }
        catch (OperationCanceledException ex)
        {
            return new PushResult(false, "Push operation canceled", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HealthPushService: failed to push health snapshot");
            return new PushResult(false, $"Failed: {ex.Message}", ex);
        }
    }
}

public sealed record PushResult(bool Success, string Message, Exception? Exception = null);
