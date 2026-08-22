namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Wire-format capability manifest published for federation routing.
/// </summary>
public sealed record NodeCapabilityManifest
{
    /// <summary>Unique identifier of this node.</summary>
    public string NodeId { get; init; } = string.Empty;

    /// <summary>Capability tier of this node.</summary>
    public NodeTier Tier { get; init; }

    /// <summary>Host platform of the runtime.</summary>
    public PlatformType Platform { get; init; } = PlatformType.Unknown;

    /// <summary>Model identifiers currently loaded and ready for inference.</summary>
    public string[] HotModelIds { get; init; } = [];

    /// <summary>Model identifiers available on disk or via registry but not necessarily loaded.</summary>
    public string[] AvailableModelIds { get; init; } = [];

    /// <summary>Task capabilities this node can satisfy.</summary>
    public TaskCapability[] SupportedCapabilities { get; init; } = [];

    /// <summary>Whether this node is accepting federated remote work.</summary>
    public bool AcceptingRemoteWork { get; init; }

    /// <summary>UTC timestamp when this manifest was generated.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
