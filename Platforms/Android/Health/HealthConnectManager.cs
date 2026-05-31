#if ANDROID
// STUB IMPLEMENTATION — returns zero/empty values for all metrics.
//
// Real Health Connect SDK calls are blocked by four compounding issues with
// Xamarin.AndroidX.Health.Connect.ConnectClient v1.1.0.2:
//
// BLOCKER 1 — NuGet version conflict
//   HC pulls Xamarin.AndroidX.Lifecycle.LiveData.Core >= 2.10.0.2.
//   CommunityToolkit.Maui 12.3.0 → MAUI Core 9.0.120 → Lifecycle.LiveData 2.8.7.3
//   constrains LiveData.Core to < 2.8.8. Ranges are incompatible (NU1107 error).
//   Pinning LiveData.Core 2.10.0.2 directly resolves NU1107 but exposes Blocker 2.
//
// BLOCKER 2 — minSdk cascade
//   Lifecycle.Runtime 2.10.0.2 (from Blocker 1 fix) requires minSdkVersion 23.
//   The Health Connect AAR (androidx.health.connect.client) itself requires minSdkVersion 26.
//   This project targets minSdkVersion 21 (Android 5.0).
//   tools:overrideLibrary can force past this but "may lead to runtime failures".
//
// BLOCKER 3 — GMS duplicate namespace conflict
//   HC transitively pulls multiple Google Play Services AARs that each bundle the same
//   base GMS packages: gms.base, gms.common, gms.location, gms.tasks appear twice.
//   aapt2 emits "Namespace '...' used in: X, X" warnings that the Android SDK build
//   system (Microsoft.Android.Sdk 35.0.105, XAAMM0000) promotes to hard errors.
//   tools:overrideLibrary does not suppress namespace-conflict warnings.
//
// BLOCKER 4 — Binding API: raw IContinuation, no Task<T> wrappers
//   The generated C# interface exposes Kotlin suspend functions as raw continuation methods:
//     IHealthConnectClient.ReadRecords(ReadRecordsRequest, IContinuation) : Object
//     IPermissionController.GetGrantedPermissions(IContinuation) : Object
//   ReadRecordsRequest also requires Kotlin.Reflect.IKClass (not Java.Lang.Class):
//     new ReadRecordsRequest(JvmClassMappingKt.GetKotlinClass(javaClass), timeFilter, ...)
//   A TaskCompletionSource<T> → IContinuation bridge (not yet written) is needed to
//   call these from C# async methods, plus careful handling of Kotlin.Result boxing.
//
// HOW TO UNBLOCK (order matters):
//   1. Upgrade CommunityToolkit.Maui to a version that allows Lifecycle.LiveData.Core >= 2.10.
//   2. Bump minSdkVersion to 26 (Android 8.0 Oreo) — acceptable for Health Connect use cases.
//   3. Pin GMS transitive packages explicitly to deduplicate them, or use the Gradle AAR shim
//      approach (custom binding project that excludes duplicate GMS bundles from HC's AAR).
//   4. Implement KotlinContinuationBridge.cs:
//        internal static Task<T> Invoke<T>(Action<IContinuation> call)
//        private class TaskContinuation<T> : Java.Lang.Object, IContinuation
//            Context => EmptyCoroutineContext.Instance (or Dispatchers.IO)
//            ResumeWith(Object result) → detect Kotlin.Result.Failure or unwrap raw value
//   Then uncomment the real calls below each TODO block.

using LocalAIAssistant.Services.Health;
using LocalAIAssistant.Services.Health.Models;

namespace LocalAIAssistant.Platforms.Android.Health;

public sealed class HealthConnectManager : IHealthConnectManager
{
    // Permission strings must match <uses-permission> declarations in AndroidManifest.xml.
    internal static readonly string[] RequiredPermissions =
    [
          "android.permission.health.READ_STEPS"
        , "android.permission.health.READ_SLEEP"
        , "android.permission.health.READ_HEART_RATE"
        , "android.permission.health.READ_DISTANCE"
    ];

    public Task<StepCountResult> GetStepCountAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (Blockers 1-4): replace with real SDK call.
        // var kClass  = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
        //                   Java.Lang.Class.FromType(typeof(StepsRecord)));
        // var filter  = TimeRangeFilter.Between(
        //                   Java.Time.Instant.OfEpochMilli(from.ToUnixTimeMilliseconds()),
        //                   Java.Time.Instant.OfEpochMilli(to.ToUnixTimeMilliseconds()));
        // var request = new ReadRecordsRequest(kClass, filter, dataOriginFilter: null,
        //                                      ascendingOrder: true, pageSize: 1000, pageToken: null);
        // var response = await KotlinContinuationBridge.Invoke<ReadRecordsResponse>(
        //                    cont => GetClient().ReadRecords(request, cont));
        // var steps = response.Records.Cast<StepsRecord>().Sum(record => (long)record.Count);
        // return new StepCountResult { Steps = steps };
        return Task.FromResult(new StepCountResult { Steps = 0 });
    }

    public Task<SleepResult> GetSleepAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (Blockers 1-4): same pattern as GetStepCountAsync.
        // Record type : SleepSessionRecord
        // Duration    : (session.EndTime.ToEpochMilli() - session.StartTime.ToEpochMilli()) / 60_000L
        // Aggregate   : TotalMinutes = sum; Sessions = records.Count
        return Task.FromResult(new SleepResult { TotalMinutes = 0, Sessions = 0 });
    }

    public Task<HeartRateResult> GetHeartRateAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (Blockers 1-4): same pattern as GetStepCountAsync.
        // Record type : HeartRateRecord
        // Samples     : record.Samples.Cast<HeartRateRecord.Sample>()
        // BPM field   : (int)sample.BeatsPerMinute (long → int)
        // Aggregate   : AverageBpm, MinBpm, MaxBpm, Samples = total sample count
        return Task.FromResult(new HeartRateResult { AverageBpm = 0, Samples = 0 });
    }

    public Task<DistanceResult> GetDistanceAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // TODO (Blockers 1-4): same pattern as GetStepCountAsync.
        // Record type : DistanceRecord
        // Distance    : record.Distance.InMeters (double)
        // Aggregate   : sum across all records
        return Task.FromResult(new DistanceResult { Metres = 0 });
    }
}
#endif
