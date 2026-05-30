namespace LocalAIAssistant.Services.Health.Models;

public record StepCountResult
{
    public long    Steps          { get; init; }
    public double? DistanceMetres { get; init; }
    public int?    ActiveCalories { get; init; }
}

public record SleepResult
{
    public int  TotalMinutes { get; init; }
    public int? DeepMinutes  { get; init; }
    public int? RemMinutes   { get; init; }
    public int? LightMinutes { get; init; }
    public int  Sessions     { get; init; }
}

public record HeartRateResult
{
    public int  AverageBpm  { get; init; }
    public int? MinBpm      { get; init; }
    public int? MaxBpm      { get; init; }
    public int? RestingBpm  { get; init; }
    public int  Samples     { get; init; }
}

public record DistanceResult
{
    public double Metres         { get; init; }
    public long?  Steps          { get; init; }
    public int?   ActiveCalories { get; init; }
}
