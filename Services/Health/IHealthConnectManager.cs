using LocalAIAssistant.Services.Health.Models;

namespace LocalAIAssistant.Services.Health;

public interface IHealthConnectManager
{
    Task<bool> CheckPermissionsAsync  (CancellationToken ct = default);
    Task       RequestPermissionsAsync(CancellationToken ct = default);

    Task<StepCountResult> GetStepCountAsync (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<SleepResult>     GetSleepAsync     (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<HeartRateResult> GetHeartRateAsync (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<DistanceResult>  GetDistanceAsync  (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
