namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Worker pool type targeted by dispatch.
/// </summary>
public enum PipelineWorkerType
{
    /// <summary>Reproducible, rule-based worker pool.</summary>
    Deterministic,

    /// <summary>LLM-backed agentic worker pool.</summary>
    Agentic
}
