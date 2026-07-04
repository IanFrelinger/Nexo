namespace Nexo.Brick.Contracts;
/// <summary>
/// Wire DTO for network-wide usage statistics (populated by central catalog when available).
/// </summary>
public class BrickUsageStatsDto
{
    /// <summary>Total executions observed across the federated catalog.</summary>
    public long TotalExecutions { get; set; }

    /// <summary>Fraction of executions that succeeded (0.0–1.0).</summary>
    public double SuccessRate { get; set; }

    /// <summary>Mean execution duration in milliseconds.</summary>
    public double AvgDurationMs { get; set; }

    /// <summary>UTC timestamp of the most recent observed execution.</summary>
    public DateTimeOffset? LastUsed { get; set; }
}
