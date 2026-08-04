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

using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
using Serilog;
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
        Log.Information("HealthConnectManager.CheckPermissionsAsync starting");
        var client = GetClientOrNull();
        Log.Information("HealthConnectManager.CheckPermissionsAsync: GetClientOrNull returned client={IsNotNull}", client is not null);
        if (client is null) return false;
        var result = await HasPermissionsAsync(client);
        Log.Information("HealthConnectManager.CheckPermissionsAsync: completed with result={Result}", result);
        return result;
    }

    public async Task RequestPermissionsAsync(CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        if (client is null) return;

        if (await HasPermissionsAsync(client)) return;

        // Build the Java Set<String> the HC contract expects, then launch via the
        // ActivityResultLauncher registered in MainActivity.OnCreate.  Using the HC
        // PermissionController contract (not ActivityCompat.RequestPermissions) is required
        // on all API levels because HC permissions live in HC's own permission store, not
        // the framework's runtime-permission store.
        var permSet = new Java.Util.HashSet();
        foreach (var perm in RequiredPermissions)
            permSet.Add(new Java.Lang.String(perm));

        MainActivity.HealthPermissionLauncher?.Launch(permSet);
    }

    public async Task<StepCountResult> GetStepCountAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var client = GetClientOrNull();
        if (client is null || !await HasPermissionsAsync(client))
            return new StepCountResult { Steps = 0 };

        var kClass   = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                           Java.Lang.Class.FromType(typeof(StepsRecord)));
        var filter   = BuildTimeFilter(from, to);
        var request  = new ReadRecordsRequest(kClass, filter, new List<AndroidX.Health.Connect.Client.Records.Metadata.DataOrigin>(), true, 1000, null);
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
        var request  = new ReadRecordsRequest(kClass, filter, new List<AndroidX.Health.Connect.Client.Records.Metadata.DataOrigin>(), true, 1000, null);
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
        var request  = new ReadRecordsRequest(kClass, filter, new List<AndroidX.Health.Connect.Client.Records.Metadata.DataOrigin>(), true, 1000, null);
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
        var request  = new ReadRecordsRequest(kClass, filter, new List<AndroidX.Health.Connect.Client.Records.Metadata.DataOrigin>(), true, 1000, null);
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
            Log.Information("HealthConnectManager.HasPermissionsAsync: calling KotlinContinuationBridge.Invoke for GetGrantedPermissions");
            var grantedObj = await KotlinContinuationBridge.Invoke<Java.Lang.Object>(
                                 cont => client.PermissionController.GetGrantedPermissions(cont));

            Log.Information("HealthConnectManager.HasPermissionsAsync: KotlinContinuationBridge.Invoke returned. grantedObj type: {Type}", grantedObj?.GetType().FullName ?? "null");

            int javaSize = -1;
            string javaString = "unknown";
            if (grantedObj is Java.Lang.Object javaObj)
            {
                try
                {
                    javaString = javaObj.ToString() ?? "null";
                    var sizeMethod = global::Android.Runtime.JNIEnv.GetMethodID(javaObj.Class.Handle, "size", "()I");
                    if (sizeMethod != IntPtr.Zero)
                    {
                        javaSize = global::Android.Runtime.JNIEnv.CallIntMethod(javaObj.Handle, sizeMethod);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "HealthConnectManager.HasPermissionsAsync: JNI reflection failed");
                }
            }
            Log.Information("HealthConnectManager.HasPermissionsAsync: Java-level size={Size}, toString={String}", javaSize, javaString);

            var grantedSet = new HashSet<string?>();
            if (grantedObj is Java.Lang.IIterable iterable)
            {
                try
                {
                    var iterator = iterable.Iterator();
                    while (iterator.HasNext)
                    {
                        var item = iterator.Next();
                        if (item is not null)
                        {
                            grantedSet.Add(item.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "HealthConnectManager: Failed to iterate grantedObj via IIterable");
                }
            }

            // Fallback to JNI toArray if IIterable failed or returned empty
            if (grantedSet.Count == 0 && grantedObj is Java.Lang.Object javaObjForToArray)
            {
                try
                {
                    var toArrayMethod = global::Android.Runtime.JNIEnv.GetMethodID(javaObjForToArray.Class.Handle, "toArray", "()[Ljava/lang/Object;");
                    if (toArrayMethod != IntPtr.Zero)
                    {
                        var arrayHandle = global::Android.Runtime.JNIEnv.CallObjectMethod(javaObjForToArray.Handle, toArrayMethod);
                        if (arrayHandle != IntPtr.Zero)
                        {
                            var elements = new global::Android.Runtime.JavaArray<Java.Lang.Object>(arrayHandle, global::Android.Runtime.JniHandleOwnership.TransferLocalRef);
                            foreach (var el in elements)
                            {
                                if (el is not null)
                                {
                                    grantedSet.Add(el.ToString());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "HealthConnectManager: Failed to marshal SetBuilder via JNI toArray");
                }
            }

            Log.Information("HealthConnectManager.HasPermissionsAsync: Granted permissions count={Count}. Set: {Set}", grantedSet.Count, string.Join(", ", grantedSet));

            var missing = RequiredPermissions.Where(p => !grantedSet.Contains(p)).ToList();
            if (missing.Any())
            {
                Log.Warning("HealthConnectManager.HasPermissionsAsync: Missing permissions: {Missing}", string.Join(", ", missing));
            }
            else
            {
                Log.Information("HealthConnectManager.HasPermissionsAsync: All required permissions are granted.");
            }

            return RequiredPermissions.All(grantedSet.Contains);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "HealthConnectManager.HasPermissionsAsync failed with exception");
            return false;
        }
    }
}
#endif