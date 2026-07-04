namespace Nexo.Brick.Contracts.Capabilities;

/// <summary>
/// Capability manifest published by each node to assist routing decisions.
/// </summary>
public sealed class NodeCapabilityManifestDto
{
    /// <summary>Stable node identifier matching usage reports and federation records.</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Compute tier of this node.</summary>
    public NodeTierDto Tier { get; set; } = NodeTierDto.Nano;

    /// <summary>Operating system platform of this node.</summary>
    public PlatformTypeDto Platform { get; set; } = PlatformTypeDto.Unknown;

    /// <summary>Model ids currently loaded and ready for inference.</summary>
    public string[] HotModelIds { get; set; } = [];

    /// <summary>Model ids installed but not necessarily loaded.</summary>
    public string[] AvailableModelIds { get; set; } = [];

    /// <summary>Task categories this node accepts for remote execution.</summary>
    public TaskCapabilityDto[] SupportedCapabilities { get; set; } = [];

    /// <summary>When false, the node is not accepting federated remote work.</summary>
    public bool AcceptingRemoteWork { get; set; }

    /// <summary>UTC timestamp when this manifest snapshot was generated.</summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
