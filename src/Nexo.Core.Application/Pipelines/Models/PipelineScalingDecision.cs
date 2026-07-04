namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Decision emitted by a scaling policy.
/// </summary>
public sealed record PipelineScalingDecision
{
    /// <summary>Recommended number of deterministic workers.</summary>
    public int DeterministicWorkers { get; init; }

    /// <summary>Recommended number of agentic workers.</summary>
    public int AgenticWorkers { get; init; }

    /// <summary>Optional max parallelism cap for the deterministic pool.</summary>
    public int? DeterministicMaxDegreeOfParallelism { get; init; }

    /// <summary>Optional max parallelism cap for the agentic pool.</summary>
    public int? AgenticMaxDegreeOfParallelism { get; init; }
}
