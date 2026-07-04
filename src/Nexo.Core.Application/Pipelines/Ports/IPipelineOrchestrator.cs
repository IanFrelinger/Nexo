using Nexo.Core.Application.Pipelines.Models;

namespace Nexo.Core.Application.Pipelines.Ports;

/// <summary>
/// Orchestrates full lifecycle execution for pipeline templates.
/// </summary>
public interface IPipelineOrchestrator
{
    /// <summary>
    /// Executes or resumes a pipeline run from the given request.
    /// </summary>
    /// <param name="request">Execution or resume parameters and template.</param>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    /// <returns>The completed or in-progress <see cref="PipelineRun"/> aggregate.</returns>
    Task<PipelineRun> RunAsync(
        PipelineExecutionRequest request,
        CancellationToken cancellationToken = default);
}
