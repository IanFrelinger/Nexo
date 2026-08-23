namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// Metadata about a brick (author, license, repository, etc.).
/// </summary>
public class BrickMetadata
{
    /// <summary>Author or publishing organization.</summary>
    public string? Author { get; init; }

    /// <summary>SPDX or human-readable license identifier.</summary>
    public string? License { get; init; }

    /// <summary>Source repository URL, when applicable.</summary>
    public string? Repository { get; init; }

    /// <summary>Local execution count maintained by the publishing node.</summary>
    public long UsageCount { get; set; }

    /// <summary>UTC timestamp of the last metadata or implementation update.</summary>
    public DateTime? LastUpdated { get; init; }
}
