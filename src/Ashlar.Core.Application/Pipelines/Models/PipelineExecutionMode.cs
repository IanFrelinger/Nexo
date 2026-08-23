namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Execution mode for a pipeline stage.
/// </summary>
public enum PipelineExecutionMode
{
    /// <summary>Stage runs on deterministic, reproducible workers.</summary>
    Deterministic,

    /// <summary>Stage runs on agentic workers with LLM-driven behavior.</summary>
    Agentic,

    /// <summary>Stage may use deterministic or agentic workers based on routing.</summary>
    Hybrid
}
