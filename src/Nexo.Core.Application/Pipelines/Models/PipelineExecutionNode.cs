namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Materialized execution node from template decomposition.
/// </summary>
public sealed record PipelineExecutionNode
{
    /// <summary>Stage identifier for this node.</summary>
    public string StageId { get; init; } = string.Empty;

    /// <summary>Execution mode governing worker selection.</summary>
    public PipelineExecutionMode Mode { get; init; }

    /// <summary>Logical graph role of this stage.</summary>
    public PipelineStageType StageType { get; init; }

    /// <summary>Fan-in join strategy when multiple predecessors exist.</summary>
    public PipelineJoinStrategy? JoinStrategy { get; init; }

    /// <summary>Quorum threshold when <see cref="JoinStrategy"/> is <see cref="PipelineJoinStrategy.Quorum"/>.</summary>
    public int? QuorumCount { get; init; }

    /// <summary>Stage identifiers that must complete before this node runs.</summary>
    public IReadOnlyList<string> Predecessors { get; init; } = Array.Empty<string>();

    /// <summary>Stage identifiers that run after this node completes.</summary>
    public IReadOnlyList<string> Successors { get; init; } = Array.Empty<string>();

    /// <summary>Ordered worker types to try on dispatch failure.</summary>
    public IReadOnlyList<PipelineWorkerType> FallbackChain { get; init; } = Array.Empty<PipelineWorkerType>();
}
