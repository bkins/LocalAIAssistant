using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.CognitivePlatform.DTOs;

namespace LocalAIAssistant.Services;

/// <summary>
/// Fetches and caches the latest Groq rate-limit usage snapshot from the API.
/// Designed to be called after each conversation turn (headers are freshest then)
/// and also on-demand from the UI.
/// </summary>
public class UsageService
{
    private readonly ICognitivePlatformClientFactory _clientFactory;

    public GroqUsageDto? Latest { get; private set; }

    public UsageService(ICognitivePlatformClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _clientFactory.Create();
            Latest = await client.GetUsageAsync(ct);
        }
        catch
        {
            // Non-fatal — leave Latest unchanged so the UI shows stale data
            // rather than blanking out.
        }
    }
}