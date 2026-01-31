namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for network-wide usage statistics (populated by central catalog when available).
/// </summary>
public class BrickUsageStatsDto
{
    public long TotalExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AvgDurationMs { get; set; }
    public DateTimeOffset? LastUsed { get; set; }
}
