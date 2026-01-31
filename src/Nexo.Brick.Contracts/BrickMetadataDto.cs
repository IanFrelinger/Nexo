namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for brick metadata (author, license, etc.).
/// </summary>
public class BrickMetadataDto
{
    public string? Author { get; set; }
    public string? License { get; set; }
    public string? Repository { get; set; }
    public long UsageCount { get; set; }
    public DateTime? LastUpdated { get; set; }
}
