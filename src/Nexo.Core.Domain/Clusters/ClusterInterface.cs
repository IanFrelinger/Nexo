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
