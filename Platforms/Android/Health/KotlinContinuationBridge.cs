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

namespace LocalAIAssistant.Platforms.Android.Health;

internal static class KotlinContinuationBridge
{
    // Wraps one Kotlin suspend function call as a Task<TResult>.
    // Pass the lambda that forwards `cont` to the generated binding method.
    internal static Task<TResult?> Invoke<TResult>(Action<IContinuation> coroutineAction)
        where TResult : Java.Lang.Object
    {
        var tcs  = new TaskCompletionSource<TResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cont = new TaskContinuation<TResult>(tcs);
        try
        {
            coroutineAction(cont);
        }
        catch (Exception ex)
        {
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

        // Called by the coroutine runtime when the suspend function completes.
        // `result` is either the boxed TResult (success) or a Kotlin.Result$Failure wrapper (error).
        //
        // Kotlin.Result is an inline value class; Kotlin.Result.Failure is NOT exposed in the
        // C# binding. Detect failure by its JVM class name ("kotlin.Result$Failure") and extract
        // the wrapped Throwable via Java reflection.
        public void ResumeWith(Java.Lang.Object result)
        {
            if (result?.Class?.Name == "kotlin.Result$Failure")
            {
                try
                {
                    var field = result.Class.GetDeclaredField("exception");
                    field.Accessible = true;
                    var throwable = field.Get(result);
                    _tcs.TrySetException(
                        new Exception($"Health Connect operation failed: {throwable}"));
                }
                catch (Exception reflectionEx)
                {
                    _tcs.TrySetException(reflectionEx);
                }
            }
            else
            {
                _tcs.TrySetResult(result as TResult);
            }
        }
    }
}
#endif
