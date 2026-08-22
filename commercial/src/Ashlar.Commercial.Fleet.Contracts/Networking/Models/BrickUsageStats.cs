namespace Ashlar.Commercial.Fleet.Contracts.Networking.Models;
/// <summary>
/// Aggregated usage statistics for a brick (for routing and cache TTL decisions).
/// </summary>
public sealed record BrickUsageStats
{
    /// <summary>Total number of executions in the window.</summary>
    public long TotalExecutions { get; init; }

    /// <summary>Fraction of executions that succeeded (0.0–1.0).</summary>
    public double SuccessRate { get; init; }

    /// <summary>Average execution duration in milliseconds.</summary>
    public double AvgDurationMs { get; init; }

    /// <summary>Last time the brick was executed.</summary>
    public DateTimeOffset? LastUsed { get; init; }

    /// <summary>Executions per hour in the rolling 1-hour window.</summary>
    public double ExecutionsPerHour { get; init; }
}
