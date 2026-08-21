#if ANDROID
// Bridges Kotlin suspend functions (exposed by the Xamarin.AndroidX.Health.Connect binding
// as IContinuation-based methods) into C# Task<T>.
//
// The HC binding does NOT generate Task<T> wrappers for Kotlin suspend functions.
// Instead, every suspend fun is exposed as:
//   SomeResult Foo(SomeArgs args, IContinuation continuation)
//
// This helper implements IContinuation so callers can use await:
//   var result = await KotlinContinuationBridge.Invoke<ReadRecordsResponse>(
//                    cont => client.ReadRecords(request, cont));
//
// Threading: the HC SDK runs the coroutine on its own dispatcher (Dispatchers.IO).
// We use EmptyCoroutineContext so the dispatcher is inherited from the HC client's
// internal scope — do NOT call this on the UI thread without Task.Run.

using Kotlin.Coroutines;
using Serilog;

namespace LocalAIAssistant.Platforms.Android.Health;

internal static class KotlinContinuationBridge
{
    // Wraps one Kotlin suspend function call as a Task<TResult>.
    // Pass the lambda that forwards `cont` to the generated binding method.
    internal static Task<TResult?> Invoke<TResult>(Func<IContinuation, Java.Lang.Object?> coroutineFunc)
        where TResult : Java.Lang.Object
    {
        Log.Information("KotlinContinuationBridge.Invoke: starting coroutineFunc");
        var tcs  = new TaskCompletionSource<TResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cont = new TaskContinuation<TResult>(tcs);
        try
        {
            var result = coroutineFunc(cont);
            Log.Information("KotlinContinuationBridge.Invoke: coroutineFunc returned result class: {ResultClass}", result?.Class?.Name ?? "null");
            
            if (result is not null && result.Class.Name == "kotlin.coroutines.intrinsics.CoroutineSingletons")
            {
                Log.Information("KotlinContinuationBridge.Invoke: asynchronous suspension (waiting for ResumeWith)");
            }
            else
            {
                Log.Information("KotlinContinuationBridge.Invoke: synchronous completion detected. Completing Task with result.");
                cont.CompleteWithSynchronousResult(result);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "KotlinContinuationBridge.Invoke: coroutineFunc threw an immediate exception");
            tcs.TrySetException(ex);
        }
        return tcs.Task;
    }

    // Implements Kotlin's Continuation<T> interface, bridging ResumeWith into a TCS.
    private sealed class TaskContinuation<TResult> : Java.Lang.Object, IContinuation
        where TResult : Java.Lang.Object
    {
        private readonly TaskCompletionSource<TResult?> _tcs;

        internal TaskContinuation(TaskCompletionSource<TResult?> tcs) => _tcs = tcs;

        // Coroutines will use whatever dispatcher the HC SDK uses internally.
        public ICoroutineContext Context => EmptyCoroutineContext.Instance;

        public void CompleteWithSynchronousResult(Java.Lang.Object? result)
        {
            if (result is null)
            {
                _tcs.TrySetResult(null);
            }
            else if (result.Class?.Name == "kotlin.Result$Failure")
            {
                var exception = ExtractException(result);
                Log.Error(exception, "KotlinContinuationBridge: synchronous failure result");
                _tcs.TrySetException(exception);
            }
            else
            {
                _tcs.TrySetResult(result as TResult);
            }
        }

        // Called by the coroutine runtime when the suspend function completes.
        // `result` is either the boxed TResult (success) or a Kotlin.Result$Failure wrapper (error).
        public void ResumeWith(Java.Lang.Object result)
        {
            Log.Information("KotlinContinuationBridge.ResumeWith called. Result class: {ClassName}", result?.Class?.Name ?? "null");
            if (result?.Class?.Name == "kotlin.Result$Failure")
            {
                var exception = ExtractException(result);
                Log.Error(exception, "KotlinContinuationBridge.ResumeWith: operation failed");
                _tcs.TrySetException(exception);
            }
            else
            {
                Log.Information("KotlinContinuationBridge.ResumeWith: operation succeeded");
                _tcs.TrySetResult(result as TResult);
            }
        }

        private static Exception ExtractException(Java.Lang.Object result)
        {
            try
            {
                var field = result.Class?.GetDeclaredField("exception");
                if (field is not null)
                {
                    field.Accessible = true;
                    var throwable = field.Get(result);
                    if (throwable is not null)
                    {
                        return new Exception($"Health Connect operation failed: {throwable}");
                    }
                }
            }
            catch
            {
                // Fall back to scanning fields or string conversion
            }

            try
            {
                var fields = result.Class?.GetDeclaredFields();
                if (fields is not null)
                {
                    foreach (var field in fields)
                    {
                        try
                        {
                            field.Accessible = true;
                            var value = field.Get(result);
                            if (value is not null)
                            {
                                return new Exception($"Health Connect operation failed: {value}");
                            }
                        }
                        catch
                        {
                            // Ignore individual field access exceptions
                        }
                    }
                }
            }
            catch
            {
                // Ignore general reflection exceptions
            }

            return new Exception($"Health Connect operation failed: {result}");
        }
    }
}
#endif
