namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Optional worker routing hints per stage.
/// </summary>
public sealed record PipelineWorkerBinding
{
    /// <summary>Target pool identifier for deterministic workers.</summary>
    public string? DeterministicPoolId { get; init; }

    /// <summary>Target pool identifier for agentic workers.</summary>
    public string? AgenticPoolId { get; init; }
}
