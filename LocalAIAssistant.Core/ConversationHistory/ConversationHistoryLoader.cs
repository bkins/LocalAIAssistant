namespace LocalAIAssistant.Core.ConversationHistory;

public static class ConversationHistoryLoader
{
    public static async Task<IReadOnlyList<T>> LoadAsync<T>(Func<Task<IReadOnlyList<T>>> loadServer
                                                         , Func<Task<IReadOnlyList<T>>> loadLocal
                                                         , Action<Exception> onServerFailure)
    {
        try
        {
            // A successful empty response is authoritative, not an offline signal.
            return await loadServer();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            onServerFailure(exception);
            return await loadLocal();
        }
    }
}
