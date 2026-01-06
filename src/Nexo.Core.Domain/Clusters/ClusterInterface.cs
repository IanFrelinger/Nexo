using Nexo.Core.Domain.Behaviors;

namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// What this cluster exposes to the outside world.
/// </summary>
public class ClusterInterface
{
    /// <summary>
    /// Inputs the cluster needs from outside (mapped to internal brick inputs).
    /// </summary>
    public IReadOnlyList<ClusterPort> Inputs { get; init; } = [];
    
    /// <summary>
    /// Outputs the cluster provides to outside (mapped from internal brick outputs).
    /// </summary>
    public IReadOnlyList<ClusterPort> Outputs { get; init; } = [];
    
    /// <summary>
    /// Events this cluster can emit.
    /// </summary>
    public IReadOnlyList<ClusterEvent> Events { get; init; } = [];
    
    /// <summary>
    /// What to do when a brick fails.
    /// </summary>
    public FailurePolicy FailurePolicy { get; init; } = FailurePolicy.Abort;
}

/// <summary>
/// Input or output port of a cluster.
/// </summary>
public class ClusterPort
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Description { get; init; } = default!;
    public bool Required { get; init; } = true;
    public object? Default { get; init; }
    
    /// <summary>
    /// Which internal brick and port this maps to.
    /// Format: "brickLocalId.portName"
    /// </summary>
    public string InternalMapping { get; init; } = default!;
}

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

