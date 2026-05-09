using Nexo.BrickContracts.Capabilities;

namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for a single brick in the catalog.
/// </summary>
public class BrickCatalogEntryDto
{
    public string WireFormatVersion { get; set; } = global::Nexo.BrickContracts.WireFormatVersion.Current;
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

    /// <summary>Codified domain knowledge for federation and operator UIs.</summary>
    public DomainKnowledgeDto? DomainKnowledge { get; set; }

    /// <summary>Network-wide usage statistics (populated by central catalog when available).</summary>
    public BrickUsageStatsDto? UsageStats { get; set; }

    /// <summary>Optional capability manifest for the host publishing this brick.</summary>
    public NodeCapabilityManifestDto? HostCapabilities { get; set; }
}
