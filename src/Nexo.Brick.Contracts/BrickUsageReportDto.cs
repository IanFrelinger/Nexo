namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for a batch of usage records from a node (POST /api/bricks/usage).
/// </summary>
public class BrickUsageReportDto
{
    public string WireFormatVersion { get; set; } = "2025-01";
    public string NodeId { get; set; } = default!;
    public List<BrickUsageRecordDto> Records { get; set; } = new();
}
