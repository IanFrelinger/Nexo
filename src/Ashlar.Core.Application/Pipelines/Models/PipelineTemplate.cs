namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Immutable template describing a composable pipeline graph.
/// </summary>
public sealed record PipelineTemplate
{
    /// <summary>Unique template identifier.</summary>
    public string TemplateId { get; init; } = string.Empty;

    /// <summary>Semantic version of the template definition.</summary>
    public string Version { get; init; } = "1.0";

    /// <summary>Ordered stage definitions comprising the pipeline.</summary>
    public IReadOnlyList<PipelineStageDefinition> Stages { get; init; } = Array.Empty<PipelineStageDefinition>();

    /// <summary>Directed edges connecting stage identifiers.</summary>
    public IReadOnlyList<PipelineEdge> Edges { get; init; } = Array.Empty<PipelineEdge>();
}
