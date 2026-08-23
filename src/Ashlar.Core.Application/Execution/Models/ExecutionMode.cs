namespace Ashlar.Core.Application.Execution.Models;

/// <summary>
/// Runtime execution mode for a step.
/// </summary>
public enum ExecutionMode
{
    /// <summary>Step runs with reproducible, rule-based execution.</summary>
    Deterministic,

    /// <summary>Step runs with LLM-backed agentic execution.</summary>
    Agentic,
}
