namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Executable graph for scheduling and dispatch.
/// </summary>
public sealed record PipelineExecutionGraph
{
    /// <summary>Source template identifier for this graph.</summary>
    public string TemplateId { get; init; } = string.Empty;

    /// <summary>Materialized execution nodes ready for scheduling.</summary>
    public IReadOnlyList<PipelineExecutionNode> Nodes { get; init; } = Array.Empty<PipelineExecutionNode>();
}
