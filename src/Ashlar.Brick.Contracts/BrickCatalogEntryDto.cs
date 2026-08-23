using Ashlar.Brick.Contracts.Capabilities;

namespace Ashlar.Brick.Contracts;
/// <summary>
/// Wire DTO for a single brick in the catalog.
/// </summary>
public class BrickCatalogEntryDto
{
    /// <summary>Wire format version for backward-compatible deserialization.</summary>
    public string WireFormatVersion { get; set; } = global::Ashlar.Brick.Contracts.WireFormatVersion.Current;

    /// <summary>Stable brick identifier used in execute URLs and federation.</summary>
    public string Id { get; set; } = default!;

    /// <summary>Human-readable display name shown in operator UIs.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Semantic version of the brick implementation (e.g. <c>1.0.0</c>).</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Emoji or icon token for catalog and workflow composer UIs.</summary>
    public string Icon { get; set; } = "📦";

    /// <summary>Category name, e.g. "Input", "Analysis", "Generation".</summary>
    public string Category { get; set; } = default!;

    /// <summary>Short description of what the brick does.</summary>
    public string Description { get; set; } = default!;

    /// <summary>Base URL for execute (POST /api/bricks/{id}/execute). When null, use catalog host.</summary>
    public string? HostBaseUrl { get; set; }

    /// <summary>Declared input and output parameter schema for this brick.</summary>
    public BrickInterfaceDto Interface { get; set; } = new();

    /// <summary>Whether a deterministic (non-LLM) implementation is available.</summary>
    public bool HasDeterministic { get; set; }

    /// <summary>Whether an agentic (LLM-backed) implementation is available.</summary>
    public bool HasAgentic { get; set; }

    /// <summary>Authoring metadata (license, repository, local usage counters).</summary>
    public BrickMetadataDto Metadata { get; set; } = new();

    /// <summary>Codified domain knowledge for federation and operator UIs.</summary>
    public DomainKnowledgeDto? DomainKnowledge { get; set; }

    /// <summary>Network-wide usage statistics (populated by central catalog when available).</summary>
    public BrickUsageStatsDto? UsageStats { get; set; }

    /// <summary>Optional capability manifest for the host publishing this brick.</summary>
    public NodeCapabilityManifestDto? HostCapabilities { get; set; }
}
