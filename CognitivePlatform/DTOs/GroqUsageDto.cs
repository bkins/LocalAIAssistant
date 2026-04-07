namespace LocalAIAssistant.CognitivePlatform.DTOs;

/// <summary>
/// Client-side DTO mirroring the response from GET /api/system/usage.
/// </summary>
public class GroqUsageDto
{
    public bool           HasData    { get; set; }
    public DateTimeOffset CapturedAt { get; set; }

    public UsageBucket Requests { get; set; } = new();
    public UsageBucket Tokens   { get; set; } = new();
}

public class UsageBucket
{
    public int    Limit        { get; set; }
    public int    Remaining    { get; set; }
    public int    Used         { get; set; }
    public double UsagePercent { get; set; }

    /// <summary>Raw reset string from Groq, e.g. "1m30s".</summary>
    public string ResetRaw         { get; set; } = string.Empty;

    /// <summary>Formatted as "1m30s (~7:13 PM)" by the API.</summary>
    public string ResetApproxLocal { get; set; } = string.Empty;
}