namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for GET /api/bricks response.
/// </summary>
public class BrickCatalogResponseDto
{
    public string WireFormatVersion { get; set; } = "2025-01";
    public IReadOnlyList<BrickCatalogEntryDto> Bricks { get; set; } = [];
    public string? ContinuationToken { get; set; }
}
