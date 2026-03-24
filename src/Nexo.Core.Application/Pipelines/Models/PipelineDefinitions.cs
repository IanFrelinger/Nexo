namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Execution mode for a pipeline stage.
/// </summary>
public enum PipelineExecutionMode
{
    Deterministic,
    Agentic,
    Hybrid
}

/// <summary>
/// Fan-in strategy for joining predecessor stage results.
/// </summary>
public enum PipelineJoinStrategy
{
    All,
    FirstSuccess,
    Quorum
}

/// <summary>
/// Worker pool type targeted by dispatch.
/// </summary>
public enum PipelineWorkerType
{
    Deterministic,
    Agentic
}

/// <summary>
/// Logical type of a stage in the pipeline graph.
/// </summary>
public enum PipelineStageType
{
    Standard,
    FanOut,
    FanIn
}

/// <summary>
/// Constraint set for an individual stage.
/// </summary>
public sealed record PipelineStageConstraints
{
    public int? MaxParallelism { get; init; }
    public bool ForceSerialized { get; init; }
    public bool Critical { get; init; }
    public bool RequiresPostValidation { get; init; }
}

/// <summary>
/// Optional worker routing hints per stage.
/// </summary>
public sealed record PipelineWorkerBinding
{
    public string? DeterministicPoolId { get; init; }
    public string? AgenticPoolId { get; init; }
}

/// <summary>
/// Declares one stage in a template.
/// </summary>
public sealed record PipelineStageDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PipelineStageType StageType { get; init; } = PipelineStageType.Standard;
    public PipelineExecutionMode Mode { get; init; } = PipelineExecutionMode.Deterministic;
    public PipelineJoinStrategy? JoinStrategy { get; init; }
    public int? QuorumCount { get; init; }
    public IReadOnlyList<PipelineWorkerType> FallbackChain { get; init; } = Array.Empty<PipelineWorkerType>();
    public PipelineStageConstraints Constraints { get; init; } = new();
    public PipelineWorkerBinding WorkerBinding { get; init; } = new();
}

/// <summary>
/// Directed edge between stage identifiers.
/// </summary>
public sealed record PipelineEdge(string FromStageId, string ToStageId);

/// <summary>
/// Immutable template describing a composable pipeline graph.
/// </summary>
public sealed record PipelineTemplate
{
    public string TemplateId { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0";
    public IReadOnlyList<PipelineStageDefinition> Stages { get; init; } = Array.Empty<PipelineStageDefinition>();
    public IReadOnlyList<PipelineEdge> Edges { get; init; } = Array.Empty<PipelineEdge>();
}

/// <summary>
/// Materialized execution node from template decomposition.
/// </summary>
public sealed record PipelineExecutionNode
{
    public string StageId { get; init; } = string.Empty;
    public PipelineExecutionMode Mode { get; init; }
    public PipelineStageType StageType { get; init; }
    public PipelineJoinStrategy? JoinStrategy { get; init; }
    public int? QuorumCount { get; init; }
    public IReadOnlyList<string> Predecessors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Successors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PipelineWorkerType> FallbackChain { get; init; } = Array.Empty<PipelineWorkerType>();
}

/// <summary>
/// Executable graph for scheduling and dispatch.
/// </summary>
public sealed record PipelineExecutionGraph
{
    public string TemplateId { get; init; } = string.Empty;
    public IReadOnlyList<PipelineExecutionNode> Nodes { get; init; } = Array.Empty<PipelineExecutionNode>();
}

/// <summary>
/// Validation diagnostics for a template.
/// </summary>
public sealed record PipelineTemplateValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Runtime state of a pipeline run.
/// </summary>
public enum PipelineRunState
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Runtime state of a stage execution.
/// </summary>
public enum PipelineStageRunState
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Stage run state for persistence and diagnostics.
/// </summary>
public sealed record PipelineStageRun
{
    public string StageId { get; init; } = string.Empty;
    public PipelineStageRunState State { get; init; } = PipelineStageRunState.Pending;
    public int Attempt { get; init; }
    public string? WorkerId { get; init; }
    public PipelineWorkerType? WorkerType { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Pipeline run aggregate.
/// </summary>
public sealed record PipelineRun
{
    public string RunId { get; init; } = string.Empty;
    public string TemplateId { get; init; } = string.Empty;
    public PipelineRunState State { get; init; } = PipelineRunState.Pending;
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
    public IReadOnlyList<PipelineStageRun> StageRuns { get; init; } = Array.Empty<PipelineStageRun>();
}

/// <summary>
/// Decision emitted by a scaling policy.
/// </summary>
public sealed record PipelineScalingDecision
{
    public int DeterministicWorkers { get; init; }
    public int AgenticWorkers { get; init; }
    public int? DeterministicMaxDegreeOfParallelism { get; init; }
    public int? AgenticMaxDegreeOfParallelism { get; init; }
}

/// <summary>
/// Snapshot for scaling policy input.
/// </summary>
public sealed record PipelineScalingSnapshot
{
    public int DeterministicQueueDepth { get; init; }
    public int AgenticQueueDepth { get; init; }
    public double DeterministicErrorRate { get; init; }
    public double AgenticErrorRate { get; init; }
    public int CurrentDeterministicWorkers { get; init; }
    public int CurrentAgenticWorkers { get; init; }
}
