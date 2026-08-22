using Ashlar.Core.Domain.Behaviors;

namespace Ashlar.Core.Domain.Clusters;

/// <summary>
/// Event that a cluster can emit.
/// </summary>
public class ClusterEvent
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Which internal brick event this surfaces.
    /// Format: "brickLocalId.eventName"
    /// </summary>
    public string InternalMapping { get; init; } = default!;
}
