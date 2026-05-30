namespace LocalAIAssistant.Services.Health.Models;

public record StepCountResult
{
    public long Steps { get; init; }
}

public record SleepResult
{
    public int DurationMinutes { get; init; }
    public int Sessions        { get; init; }
}

public record HeartRateResult
{
    public int AverageBpm { get; init; }
    public int MinBpm     { get; init; }
    public int MaxBpm     { get; init; }
    public int Samples    { get; init; }
}

public record DistanceResult
{
    public double Meters { get; init; }
}
