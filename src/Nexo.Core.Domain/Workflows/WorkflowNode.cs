using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// A node in the workflow graph.
/// </summary>
public abstract record WorkflowNode
{
    /// <summary>Unique node identifier within the workflow graph.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Display label for the node in the composer.</summary>
    public string Name { get; init; } = "";

    /// <summary>Canvas position of this node.</summary>
    public Position Position { get; init; } = new(0, 0);
    
    /// <summary>
    /// Input ports that can receive connections.
    /// </summary>
    public IReadOnlyList<NodePort> Inputs { get; init; } = [];
    
    /// <summary>
    /// Output ports that can send data to other nodes.
    /// </summary>
    public IReadOnlyList<NodePort> Outputs { get; init; } = [];
}
