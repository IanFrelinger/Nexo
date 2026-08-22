namespace Ashlar.Brick.Contracts;
/// <summary>
/// Wire DTO for GET /api/bricks response.
/// </summary>
public class BrickCatalogResponseDto
{
    /// <summary>Wire format version for backward-compatible deserialization.</summary>
    public string WireFormatVersion { get; set; } = Ashlar.Brick.Contracts.WireFormatVersion.Current;

    /// <summary>Page of catalog entries returned by the host.</summary>
    public IReadOnlyList<BrickCatalogEntryDto> Bricks { get; set; } = [];

    /// <summary>Opaque token for fetching the next page; null when no further pages exist.</summary>
    public string? ContinuationToken { get; set; }
}
