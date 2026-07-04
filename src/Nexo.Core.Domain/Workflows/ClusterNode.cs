using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// A cluster node (pre-built combination of agents/bricks).
/// </summary>
public record ClusterNode : WorkflowNode
{
    /// <summary>Cluster definition id to instantiate.</summary>
    public string ClusterId { get; init; } = "";

    /// <summary>Default implementation mode for bricks inside the cluster.</summary>
    public ImplementationMode Mode { get; init; } = ImplementationMode.Auto;

    /// <summary>Parameter values passed into the cluster instance.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; }
        = new Dictionary<string, object>();
}
