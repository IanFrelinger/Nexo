namespace Nexo.Brick.Contracts;
/// <summary>
/// Wire DTO for a batch of usage records from a node (POST /api/bricks/usage).
/// </summary>
public class BrickUsageReportDto
{
    /// <summary>Wire format version for backward-compatible deserialization.</summary>
    public string WireFormatVersion { get; set; } = Nexo.Brick.Contracts.WireFormatVersion.Current;

    /// <summary>Reporting node identifier (stable across restarts).</summary>
    public string NodeId { get; set; } = default!;

    /// <summary>Aggregated per-brick usage records collected since the last report.</summary>
    public List<BrickUsageRecordDto> Records { get; set; } = new();
}
