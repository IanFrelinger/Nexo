using Nexo.Core.Domain.Clusters;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// A complete workflow composed of clusters and connections.
/// </summary>
public class Workflow
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
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

/// <summary>
/// A connection between cluster instances in a workflow.
/// </summary>
public class WorkflowConnection
{
    public string Id { get; init; } = default!;
    public string FromInstanceId { get; init; } = default!;
    public string FromOutput { get; init; } = default!;
    public string ToInstanceId { get; init; } = default!;
    public string ToInput { get; init; } = default!;
}

