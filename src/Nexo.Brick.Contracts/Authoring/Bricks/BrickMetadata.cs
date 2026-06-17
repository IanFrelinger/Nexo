namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Metadata about a brick (author, license, repository, etc.).
/// </summary>
public class BrickMetadata
{
    public string? Author { get; init; }
    public string? License { get; init; }
    public string? Repository { get; init; }
    public long UsageCount { get; set; }
    public DateTime? LastUpdated { get; init; }
}
