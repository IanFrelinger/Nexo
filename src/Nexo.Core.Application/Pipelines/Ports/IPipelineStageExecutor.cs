using Nexo.Core.Application.Pipelines.Models;

namespace Nexo.Core.Application.Pipelines.Ports;

/// <summary>
/// Executes one stage against an underlying worker implementation.
/// </summary>
public interface IPipelineStageExecutor
{
    /// <summary>
    /// Worker pool type this executor dispatches to.
    /// </summary>
    PipelineWorkerType WorkerType { get; }

    /// <summary>
    /// Runs a single stage attempt and returns normalized output.
    /// </summary>
    /// <param name="request">Stage context, inputs, and attempt metadata.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>Success, retryability, and output or error from the worker.</returns>
    Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default);
}
