#if ANDROID
// STUB IMPLEMENTATION — returns plausible mock data.
//
// To replace with real Health Connect reads:
//   1. Add NuGet: Xamarin.AndroidX.Health.Connect.ConnectClient (v1.1.0.2) — Android-only.
//   2. Verify the C# binding API. Key entry points:
//        AndroidX.Health.Connect.Client.HealthConnectClient.GetOrCreate(Platform.CurrentActivity)
//        ReadRecordsRequest / ReadRecordsResponse
//        Record types: StepsRecord, SleepSessionRecord, HeartRateRecord, DistanceRecord
//   3. Replace each method body below with real async SDK calls (see commented examples).
//   4. Request permissions before the first call — use HealthConnectClient.RequestPermissionsActivityContract
//      launched from MainActivity (Health Connect permissions cannot use the standard ActivityCompat flow).

using LocalAIAssistant.Services.Health;
using LocalAIAssistant.Services.Health.Models;

namespace LocalAIAssistant.Platforms.Android.Health;

public sealed class HealthConnectManager : IHealthConnectManager
{
    public Task<StepCountResult> GetStepCountAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (real):
        // var client  = AndroidX.Health.Connect.Client.HealthConnectClient.GetOrCreate(Platform.CurrentActivity);
        // var filter  = AndroidX.Health.Connect.Client.Time.TimeRangeFilter.Between(
        //                   Java.Time.Instant.OfEpochMilli(from.ToUnixTimeMilliseconds()),
        //                   Java.Time.Instant.OfEpochMilli(to.ToUnixTimeMilliseconds()));
        // var request = new AndroidX.Health.Connect.Client.Request.ReadRecordsRequest(
        //                   Java.Lang.Class.FromType(typeof(AndroidX.Health.Connect.Client.Records.StepsRecord)),
        //                   filter, pageSize: 1000);
        // var response = await client.ReadRecordsAsync(request);
        // var steps    = response.Records
        //                        .Cast<AndroidX.Health.Connect.Client.Records.StepsRecord>()
        //                        .Sum(record => record.Count);
        // return new StepCountResult { Steps = steps };

        return Task.FromResult(new StepCountResult { Steps = 7_432 });
    }

    public Task<SleepResult> GetSleepAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (real): read SleepSessionRecord, sum session durations, count sessions
        return Task.FromResult(new SleepResult
                               {
                                   TotalMinutes = 427
                                 , Sessions     = 1
                               });
    }

    public Task<HeartRateResult> GetHeartRateAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (real): read HeartRateRecord samples, compute min/max/average BPM
        return Task.FromResult(new HeartRateResult
                               {
                                   AverageBpm = 68
                                 , MinBpm     = 52
                                 , MaxBpm     = 112
                                 , Samples    = 240
                               });
    }

    public Task<DistanceResult> GetDistanceAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (real): read DistanceRecord, sum Distance.InMeters
        return Task.FromResult(new DistanceResult { Metres = 5_920.0 });
    }
}
#endif
