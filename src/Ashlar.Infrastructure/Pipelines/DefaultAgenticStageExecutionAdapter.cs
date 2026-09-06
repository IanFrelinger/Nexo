using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Infrastructure.Pipelines;

/// <summary>
/// Default agentic adapter placeholder.
/// Replace with concrete provider-backed execution integration in environment-specific composition.
/// </summary>
public sealed class DefaultAgenticStageExecutionAdapter : IPipelineStageExecutionAdapter
{
    private readonly ILogger<DefaultAgenticStageExecutionAdapter> _logger;

    /// <summary>Initializes a new default agentic stage execution adapter.</summary>
    public DefaultAgenticStageExecutionAdapter(ILogger<DefaultAgenticStageExecutionAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Adapter key.</summary>
    public string AdapterKey => "default";

    /// <summary>Worker type.</summary>
    public PipelineWorkerType WorkerType => PipelineWorkerType.Agentic;

    /// <summary>Execute asynchronously.</summary>
    public Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Check for ASHLAR_ALLOW_MOCK=1 (CI-only test hook).
        // When set, return success to allow perf/throughput gates to run without real adapters.
        var allowMock = Environment.GetEnvironmentVariable("ASHLAR_ALLOW_MOCK");
        if (string.Equals(allowMock, "1", StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Agentic adapter '{AdapterKey}' running in mock mode (ASHLAR_ALLOW_MOCK=1); " +
                "stage {StageId} reported as succeeded for CI testing.",
                AdapterKey,
                request.StageId);

            return Task.FromResult(new PipelineStageExecutionResult
            {
                Succeeded = true,
                Retryable = false,
                WorkerId = "agentic-default-mock",
                Output = $"agentic:{request.StageId}:mock-success",
                Error = null
            });
        }

        // Placeholder, not a working engine — see DefaultDeterministicStageExecutionAdapter for
        // the full rationale. It used to fabricate success doing no work; it now fails honestly
        // so `ashlar pipeline run` does not report success for stages that never ran.
        _logger.LogWarning(
            "Agentic pipeline adapter '{AdapterKey}' is the default placeholder and performs no work; " +
            "stage {StageId} is reported as failed. Register a concrete agentic adapter to execute stages.",
            AdapterKey,
            request.StageId);

        return Task.FromResult(new PipelineStageExecutionResult
        {
            Succeeded = false,
            Retryable = false,
            WorkerId = "agentic-default",
            Output = $"agentic:{request.StageId}:no-op",
            Error = "No agentic pipeline adapter is configured; the default placeholder performs no work."
        });
    }
}
