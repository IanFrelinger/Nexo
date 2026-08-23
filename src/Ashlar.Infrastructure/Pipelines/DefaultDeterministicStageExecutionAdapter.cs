using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Infrastructure.Pipelines;

/// <summary>
/// Default deterministic adapter placeholder.
/// Replace with concrete deterministic engine integration in environment-specific composition.
/// </summary>
public sealed class DefaultDeterministicStageExecutionAdapter : IPipelineStageExecutionAdapter
{
    private readonly ILogger<DefaultDeterministicStageExecutionAdapter> _logger;

    /// <summary>Initializes a new default deterministic stage execution adapter.</summary>
    public DefaultDeterministicStageExecutionAdapter(ILogger<DefaultDeterministicStageExecutionAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Adapter key.</summary>
    public string AdapterKey => "default";

    /// <summary>Worker type.</summary>
    public PipelineWorkerType WorkerType => PipelineWorkerType.Deterministic;

    /// <summary>Execute asynchronously.</summary>
    public Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null) throw new ArgumentNullException(nameof(request));

        // This is a PLACEHOLDER, not a working engine. It used to return Succeeded=true doing
        // no work, so `ashlar pipeline run` reported success for stages that never executed.
        // It now fails, which flows through the orchestrator's failure path and finalizes the
        // run as Failed (non-zero exit) — the honest outcome for an unconfigured adapter.
        // Register a real IPipelineStageExecutionAdapter under this key to do actual work.
        // (Not thrown from the constructor: that would crash DI for every `ashlar pipeline`
        // subcommand, including validate/diagnostics, which legitimately resolve this type.)
        _logger.LogWarning(
            "Deterministic pipeline adapter '{AdapterKey}' is the default placeholder and performs no work; " +
            "stage {StageId} is reported as failed. Register a concrete deterministic adapter " +
            "(e.g. via ASHLAR_PIPELINE_DETERMINISTIC_ADAPTER) to execute stages.",
            AdapterKey,
            request.StageId);

        return Task.FromResult(new PipelineStageExecutionResult
        {
            Succeeded = false,
            Retryable = false,
            WorkerId = "deterministic-default",
            Output = $"deterministic:{request.StageId}:no-op",
            Error = "No deterministic pipeline adapter is configured; the default placeholder performs no work."
        });
    }
}
