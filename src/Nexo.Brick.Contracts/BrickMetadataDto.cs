namespace Nexo.Brick.Contracts;
/// <summary>
/// Wire DTO for brick metadata (author, license, etc.).
/// </summary>
public class BrickMetadataDto
{
    /// <summary>Author or publishing organization.</summary>
    public string? Author { get; set; }

    /// <summary>SPDX or human-readable license identifier.</summary>
    public string? License { get; set; }

    /// <summary>Source repository URL, when applicable.</summary>
    public string? Repository { get; set; }

    /// <summary>Local execution count maintained by the publishing node.</summary>
    public long UsageCount { get; set; }

    /// <summary>UTC timestamp of the last metadata or implementation update.</summary>
    public DateTime? LastUpdated { get; set; }
}
