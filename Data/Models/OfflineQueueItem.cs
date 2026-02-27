namespace LocalAIAssistant.Data.Models;

public class OfflineQueueItem
{
    public Guid    Id              { get; set; } // Local identity
    public Guid    ClientRequestId { get; set; } // Sent to API (idempotency key)
    public string  SessionId       { get; set; } = "";
    public string  Input           { get; set; } = "";
    public string? Model           { get; set; }

    public DateTime CreatedUtc { get; set; }

    public OfflineQueueStatus Status { get; set; }

    public int RetryCount { get; set; }         // Optional but useful
}

public enum OfflineQueueStatus
{
    Pending    = 0
  , Processing = 1
  , Completed  = 2
  , Failed     = 3
}

