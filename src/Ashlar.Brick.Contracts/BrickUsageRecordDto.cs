namespace Ashlar.Brick.Contracts;
/// <summary>
/// Wire DTO for a single brick's aggregated usage from a node (for usage reporting).
/// </summary>
public class BrickUsageRecordDto
{
    /// <summary>Brick identifier matching the catalog entry id.</summary>
    public string BrickId { get; set; } = default!;

    /// <summary>Number of executions observed in the reporting window.</summary>
    public long ExecutionCount { get; set; }

    /// <summary>Fraction of executions that succeeded (0.0–1.0).</summary>
    public double SuccessRate { get; set; }

    /// <summary>Mean execution duration in milliseconds.</summary>
    public double AvgDurationMs { get; set; }

    /// <summary>UTC timestamp of the most recent execution in the window.</summary>
    public DateTimeOffset LastUsed { get; set; }
}
