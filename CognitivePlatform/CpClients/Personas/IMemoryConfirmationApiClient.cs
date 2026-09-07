namespace LocalAIAssistant.CognitivePlatform.CpClients.Personas;

public interface IMemoryConfirmationApiClient
{
    Task<MemoryConfirmationRefreshResult> RefreshAsync( string            conversationId
                                                       , CancellationToken cancellationToken = default );
}

public sealed record MemoryConfirmationRefreshResult(bool IsAuthoritative, int PendingCount);
