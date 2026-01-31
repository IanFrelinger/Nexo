namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for a single brick's aggregated usage from a node (for usage reporting).
/// </summary>
public class BrickUsageRecordDto
{
    public string BrickId { get; set; } = default!;
    public long ExecutionCount { get; set; }
    public double SuccessRate { get; set; }
    public double AvgDurationMs { get; set; }
    public DateTimeOffset LastUsed { get; set; }
}
