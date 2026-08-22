using Ashlar.Brick.Contracts;
using Ashlar.Brick.Contracts.Capabilities;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Result wrapper for capabilities retrieval.
/// </summary>
public sealed record CapabilitiesFetchResult
{
    /// <summary>Retrieved node capability manifest, if available.</summary>
    public NodeCapabilityManifestDto? Capabilities { get; init; }
    /// <summary>Whether the capabilities snapshot is stale.</summary>
    public bool IsStale { get; init; }
}
