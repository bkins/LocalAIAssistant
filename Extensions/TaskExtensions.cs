using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.Extensions;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        task.ContinueWith(aTask =>
                          {
                              System.Diagnostics.Debug.WriteLine(aTask.Exception);
                          }
                        , TaskContinuationOptions.OnlyOnFaulted);
    }

    public static void FireAndForget(this Task task, ILoggingService logger)
    {
        task.ContinueWith(aTask =>
                          {
                              logger.LogError(aTask.Exception!
                                           , "Unhandled exception in fire-and-forget task");
                          }
                        , TaskContinuationOptions.OnlyOnFaulted);
    }
}