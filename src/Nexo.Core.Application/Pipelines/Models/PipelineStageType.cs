namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Logical type of a stage in the pipeline graph.
/// </summary>
public enum PipelineStageType
{
    /// <summary>Single-input, single-output stage with no fan semantics.</summary>
    Standard,

    /// <summary>Stage that fans out work to parallel successors.</summary>
    FanOut,

    /// <summary>Stage that joins results from multiple predecessors.</summary>
    FanIn
}
