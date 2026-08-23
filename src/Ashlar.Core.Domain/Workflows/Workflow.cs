using Ashlar.Core.Domain.Clusters;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// A complete workflow composed of clusters and connections.
/// </summary>
public class Workflow
{
    /// <summary>Stable workflow identifier.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable workflow name.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Short description of the workflow purpose.</summary>
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Cluster instances in this workflow.
    /// </summary>
    public IReadOnlyList<ClusterInstance> Instances { get; init; } = [];
    
    /// <summary>
    /// Connections between cluster instances.
    /// </summary>
    public IReadOnlyList<WorkflowConnection> Connections { get; init; } = [];
}
