using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Infrastructure.Pipelines;

/// <summary>
/// Adapter abstraction used by pipeline stage executors to delegate actual work.
/// </summary>
public interface IPipelineStageExecutionAdapter
{
    /// <summary>
    /// Adapter identifier used for routing, e.g. "default".
    /// </summary>
    string AdapterKey { get; }

    /// <summary>
    /// Worker type this adapter supports.
    /// </summary>
    PipelineWorkerType WorkerType { get; }

    /// <summary>
    /// Executes the stage request.
    /// </summary>
    Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default);
}
