using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// A connection between two ports in a visual workflow.
/// </summary>
public record VisualWorkflowConnection
{
    /// <summary>Unique connection identifier.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Source node id.</summary>
    public string FromNodeId { get; init; } = "";

    /// <summary>Source port id on <see cref="FromNodeId"/>.</summary>
    public string FromPortId { get; init; } = "";

    /// <summary>Target node id.</summary>
    public string ToNodeId { get; init; } = "";

    /// <summary>Target port id on <see cref="ToNodeId"/>.</summary>
    public string ToPortId { get; init; } = "";

    /// <summary>Semantic connection type for rendering and execution.</summary>
    public ConnectionType Type { get; init; } = ConnectionType.Data;
}
