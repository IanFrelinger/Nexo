namespace Nexo.Core.Application.Workflows;

/// <summary>Aggregate outcome of a completed workflow execution.</summary>
public class WorkflowResult
{
    /// <summary>Correlation id linking logs, events, and operator traces.</summary>
    public string CorrelationId { get; init; } = "";

    /// <summary>Whether the workflow completed without fatal errors.</summary>
    public bool Success { get; init; }

    /// <summary>Final outputs gathered from <see cref="Nexo.Core.Domain.Workflows.OutputNode"/> instances.</summary>
    public Dictionary<string, object> Outputs { get; init; } = new();

    /// <summary>Per-node execution results keyed by node id.</summary>
    public Dictionary<string, NodeResult> NodeResults { get; init; } = new();

    /// <summary>Roll-up execution metrics collected during the run.</summary>
    public WorkflowMetrics Metrics { get; init; } = new();
}
