#if ANDROID
// Real Health Connect SDK implementation.
// Package: Xamarin.AndroidX.Health.Connect.ConnectClient v1.1.0.2 (Android-only).
//
// Requirements:
//   - Android 8.0+ (API 26) — HC AAR declares minSdkVersion 26.
//   - Health Connect app installed — GetSdkStatus() guards every call.
//   - Permissions granted — HasPermissionsAsync() guards every call.
//     Permission dialog is triggered from SettingsPage via RequestPermissionsAsync().
//
// Binding notes (Xamarin.AndroidX.Health.Connect.ConnectClient v1.1.0.2):
//   - HealthConnectClient.GetOrCreate() returns IHealthConnectClient (not HealthConnectClient).
//   - Kotlin suspend functions are NOT wrapped as Task<T>; they expose raw IContinuation.
//     KotlinContinuationBridge.Invoke<T>() provides the Task<T> adapter.
//   - ReadRecordsRequest takes Kotlin.Reflect.IKClass (not Java.Lang.Class).
//     Obtain via: Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(Java.Lang.Class.FromType(typeof(T))).
//   - ReadRecordsResponse is in AndroidX.Health.Connect.Client.Response (not .Request).

using AndroidX.Core.App;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Response;
using AndroidX.Health.Connect.Client.Time;
using LocalAIAssistant.Services.Health;
using LocalAIAssistant.Services.Health.Models;
using Microsoft.Maui.ApplicationModel;

namespace LocalAIAssistant.Platforms.Android.Health;

public sealed class HealthConnectManager : IHealthConnectManager
{
    // Must match <uses-permission> entries in AndroidManifest.xml.
    internal static readonly string[] RequiredPermissions =
    [
          "android.permission.health.READ_STEPS"
        , "android.permission.health.READ_SLEEP"
        , "android.permission.health.READ_HEART_RATE"
        , "android.permission.health.READ_DISTANCE"
    ];

    public async Task<bool> CheckPermissionsAsync(CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        return client is not null && await HasPermissionsAsync(client);
    }

    // On Android 13+ (API 33) HC permissions are routed through the standard runtime
    // permission dialog via ActivityCompat.RequestPermissions. On API 28–32 the dialog
    // may not appear; a full ActivityResultLauncher contract is needed for those versions.
    public async Task RequestPermissionsAsync(CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        if (client is null) return;

        if (await HasPermissionsAsync(client)) return;

        var activity = Platform.CurrentActivity;
        if (activity is null) return;

        ActivityCompat.RequestPermissions(activity, RequiredPermissions, 0);
    }

    public async Task<StepCountResult> GetStepCountAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        if (client is null || !await HasPermissionsAsync(client))
            return new StepCountResult { Steps = 0 };

        var kClass   = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                           Java.Lang.Class.FromType(typeof(StepsRecord)));
        var filter   = BuildTimeFilter(from, to);
        var request  = new ReadRecordsRequest(kClass, filter, null, true, 1000, null);
        var response = await KotlinContinuationBridge.Invoke<ReadRecordsResponse>(
                           cont => client.ReadRecords(request, cont));

        if (response is null) return new StepCountResult { Steps = 0 };

        var steps = response.Records
                            .Cast<StepsRecord>()
                            .Sum(record => (long)record.Count);
        return new StepCountResult { Steps = steps };
    }

    public async Task<SleepResult> GetSleepAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        if (client is null || !await HasPermissionsAsync(client))
            return new SleepResult { TotalMinutes = 0, Sessions = 0 };

        var kClass   = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                           Java.Lang.Class.FromType(typeof(SleepSessionRecord)));
        var filter   = BuildTimeFilter(from, to);
        var request  = new ReadRecordsRequest(kClass, filter, null, true, 1000, null);
        var response = await KotlinContinuationBridge.Invoke<ReadRecordsResponse>(
                           cont => client.ReadRecords(request, cont));

        if (response is null) return new SleepResult { TotalMinutes = 0, Sessions = 0 };

        var sessions     = response.Records.Cast<SleepSessionRecord>().ToList();
        var totalMinutes = sessions.Sum(session =>
                               (int)((session.EndTime.ToEpochMilli() - session.StartTime.ToEpochMilli()) / 60_000L));
        return new SleepResult
               {
                   TotalMinutes = totalMinutes
                 , Sessions     = sessions.Count
               };
    }

    public async Task<HeartRateResult> GetHeartRateAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        if (client is null || !await HasPermissionsAsync(client))
            return new HeartRateResult { AverageBpm = 0, Samples = 0 };

        var kClass   = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                           Java.Lang.Class.FromType(typeof(HeartRateRecord)));
        var filter   = BuildTimeFilter(from, to);
        var request  = new ReadRecordsRequest(kClass, filter, null, true, 1000, null);
        var response = await KotlinContinuationBridge.Invoke<ReadRecordsResponse>(
                           cont => client.ReadRecords(request, cont));

        if (response is null) return new HeartRateResult { AverageBpm = 0, Samples = 0 };

        var bpmSamples = response.Records
                                 .Cast<HeartRateRecord>()
                                 .SelectMany(record => record.Samples.Cast<HeartRateRecord.Sample>())
                                 .Select(sample => (int)sample.BeatsPerMinute)
                                 .ToList();

        if (bpmSamples.Count == 0)
            return new HeartRateResult { AverageBpm = 0, Samples = 0 };

        return new HeartRateResult
               {
                   AverageBpm = (int)bpmSamples.Average()
                 , MinBpm     = bpmSamples.Min()
                 , MaxBpm     = bpmSamples.Max()
                 , Samples    = bpmSamples.Count
               };
    }

    public async Task<DistanceResult> GetDistanceAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        if (client is null || !await HasPermissionsAsync(client))
            return new DistanceResult { Metres = 0 };

        var kClass   = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                           Java.Lang.Class.FromType(typeof(DistanceRecord)));
        var filter   = BuildTimeFilter(from, to);
        var request  = new ReadRecordsRequest(kClass, filter, null, true, 1000, null);
        var response = await KotlinContinuationBridge.Invoke<ReadRecordsResponse>(
                           cont => client.ReadRecords(request, cont));

        if (response is null) return new DistanceResult { Metres = 0 };

        var metres = response.Records
                             .Cast<DistanceRecord>()
                             .Sum(record => record.Distance.Meters);
        return new DistanceResult { Metres = metres };
    }

    private static TimeRangeFilter BuildTimeFilter(DateTimeOffset from, DateTimeOffset to)
        => TimeRangeFilter.Between(
               Java.Time.Instant.OfEpochMilli(from.ToUnixTimeMilliseconds()),
               Java.Time.Instant.OfEpochMilli(to.ToUnixTimeMilliseconds()));

    // Returns null when Health Connect is not installed or unavailable.
    // SdkAvailable=1, SdkUnavailable=2, SdkUnavailableProviderUpdateRequired=3.
    internal static IHealthConnectClient? GetClientOrNull()
    {
        try
        {
            var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
            if (HealthConnectClient.GetSdkStatus(context) != HealthConnectClient.SdkAvailable)
                return null;
            return HealthConnectClient.GetOrCreate(context);
        }
        catch
        {
            return null;
        }
    }

    // Returns false if the permission check fails — callers return empty results.
    private static async Task<bool> HasPermissionsAsync(IHealthConnectClient client)
    {
        try
        {
            var grantedObj = await KotlinContinuationBridge.Invoke<Java.Lang.Object>(
                                 cont => client.PermissionController.GetGrantedPermissions(cont));

            var grantedSet = (grantedObj as System.Collections.IEnumerable)
                                 ?.Cast<object>()
                                 .Select(item => item?.ToString())
                                 .Where(item => item is not null)
                                 .ToHashSet()
                             ?? new HashSet<string?>();

            return RequiredPermissions.All(grantedSet.Contains);
        }
        catch
        {
            return false;
        }
    }
}
#endif