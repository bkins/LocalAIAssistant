namespace LocalAIAssistant.CognitivePlatform.CpClients.MemoryReview;

public interface IMemoryReviewApiClient
{
    Task<MemoryReviewDto?> GetReviewAsync(CancellationToken cancellationToken = default);
}
