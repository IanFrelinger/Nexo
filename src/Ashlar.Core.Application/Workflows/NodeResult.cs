namespace Ashlar.Core.Application.Workflows;

/// <summary>Outcome of executing a single workflow node.</summary>
public class NodeResult
{
    /// <summary>Workflow node id that produced this result.</summary>
    public string NodeId { get; init; } = "";

    /// <summary>Whether the node completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Named outputs emitted by the node for downstream connections.</summary>
    public Dictionary<string, object> Outputs { get; init; } = new();

    /// <summary>Optional per-node timing and implementation metrics.</summary>
    public NodeMetrics? Metrics { get; init; }
}
