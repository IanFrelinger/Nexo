namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Input request for executing or resuming a pipeline run.
/// </summary>
public sealed record PipelineExecutionRequest
{
    public string? RunId { get; init; }
    public string? ResumeRunId { get; init; }
    public bool ResumeFailedStages { get; init; }
    public IReadOnlyDictionary<string, string> Inputs { get; init; } = new Dictionary<string, string>();
    public required PipelineTemplate Template { get; init; }
}

/// <summary>
/// Context payload passed to a stage executor.
/// </summary>
public sealed record PipelineStageExecutionRequest
{
    public required string RunId { get; init; }
    public required string StageId { get; init; }
    public required int Attempt { get; init; }
    public required PipelineWorkerType WorkerType { get; init; }
    public required PipelineStageDefinition Stage { get; init; }
    public required PipelineExecutionNode Node { get; init; }
    public IReadOnlyDictionary<string, string> Inputs { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Normalized output from a stage executor.
/// </summary>
public sealed record PipelineStageExecutionResult
{
    public bool Succeeded { get; init; }
    public bool Retryable { get; init; } = true;
    public string? WorkerId { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
}
