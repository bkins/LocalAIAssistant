using LocalAIAssistant.Services.Health.Models;

namespace LocalAIAssistant.Services.Health;

public interface IHealthConnectManager
{
    Task<StepCountResult> GetStepCountAsync (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<SleepResult>     GetSleepAsync     (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<HeartRateResult> GetHeartRateAsync (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<DistanceResult>  GetDistanceAsync  (DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
