namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for a single brick in the catalog.
/// </summary>
public class BrickCatalogEntryDto
{
    public string WireFormatVersion { get; set; } = "2025-01";
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Version { get; set; } = "1.0.0";
    public string Icon { get; set; } = "📦";
    /// <summary>Category name, e.g. "Input", "Analysis", "Generation".</summary>
    public string Category { get; set; } = default!;
    public string Description { get; set; } = default!;
    /// <summary>Base URL for execute (POST /api/bricks/{id}/execute). When null, use catalog host.</summary>
    public string? HostBaseUrl { get; set; }
    public BrickInterfaceDto Interface { get; set; } = new();
    public bool HasDeterministic { get; set; }
    public bool HasAgentic { get; set; }
    public BrickMetadataDto Metadata { get; set; } = new();
}
