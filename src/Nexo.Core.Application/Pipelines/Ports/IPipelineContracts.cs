using Nexo.Core.Application.Pipelines.Models;

namespace Nexo.Core.Application.Pipelines.Ports;

/// <summary>
/// Validates pipeline templates and returns diagnostics.
/// </summary>
public interface IPipelineTemplateValidator
{
    PipelineTemplateValidationResult Validate(PipelineTemplate template);
}

/// <summary>
/// Decomposes a validated template into an execution graph.
/// </summary>
public interface IPipelineDecomposer
{
    PipelineExecutionGraph Decompose(PipelineTemplate template);
}

/// <summary>
/// Evaluates whether a stage can be scheduled based on predecessor state.
/// </summary>
public interface IPipelineScheduler
{
    IReadOnlyList<string> GetReadyStages(
        PipelineExecutionGraph graph,
        IReadOnlyDictionary<string, PipelineStageRunState> states);
}

/// <summary>
/// Persists pipeline runs for diagnostics and recovery.
/// </summary>
public interface IPipelineRunStore
{
    Task SaveAsync(PipelineRun run, CancellationToken cancellationToken = default);
    Task<PipelineRun?> GetAsync(string runId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Produces scaling decisions for deterministic and agentic worker pools.
/// </summary>
public interface IPipelineScalingPolicy
{
    PipelineScalingDecision Evaluate(PipelineScalingSnapshot snapshot);
}

/// <summary>
/// Executes one stage against an underlying worker implementation.
/// </summary>
public interface IPipelineStageExecutor
{
    PipelineWorkerType WorkerType { get; }

    Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Orchestrates full lifecycle execution for pipeline templates.
/// </summary>
public interface IPipelineOrchestrator
{
    Task<PipelineRun> RunAsync(
        PipelineExecutionRequest request,
        CancellationToken cancellationToken = default);
}
