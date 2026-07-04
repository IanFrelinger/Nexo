using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Executes deterministic stages.
/// </summary>
public sealed class DeterministicPipelineStageExecutor : IPipelineStageExecutor
{
    private readonly ILogger<DeterministicPipelineStageExecutor> _logger;
    private readonly PipelineExecutionOptions _executionOptions;
    private readonly IPipelineStageExecutionAdapter _adapter;

    /// <summary>Initializes a new deterministic pipeline stage executor.</summary>
    public DeterministicPipelineStageExecutor(
        ILogger<DeterministicPipelineStageExecutor> logger,
        IEnumerable<IPipelineStageExecutionAdapter> adapters,
        IOptions<PipelineExecutionAdapterOptions> adapterOptions,
        IOptions<PipelineExecutionOptions> executionOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executionOptions = executionOptions?.Value ?? throw new ArgumentNullException(nameof(executionOptions));

        if (adapters == null) throw new ArgumentNullException(nameof(adapters));
        var adapterKey = adapterOptions?.Value?.DeterministicAdapter ?? "default";
        _adapter = adapters.FirstOrDefault(x =>
                x.WorkerType == PipelineWorkerType.Deterministic &&
                string.Equals(x.AdapterKey, adapterKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No deterministic pipeline adapter registered for key '{adapterKey}'.");
    }

    /// <summary>Worker type.</summary>
    public PipelineWorkerType WorkerType => PipelineWorkerType.Deterministic;

    /// <summary>Execute asynchronously.</summary>
    public Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Optional test hook (disabled in production by default).
        var fail = _executionOptions.EnableTestHooks && ShouldFail(request, "deterministic");
        if (fail)
        {
            _logger.LogWarning("Deterministic stage {StageId} failed by test hook.", request.StageId);
            return Task.FromResult(new PipelineStageExecutionResult
            {
                Succeeded = false,
                Retryable = true,
                WorkerId = "deterministic-test-hook",
                Error = "Deterministic execution failed (test hook)."
            });
        }

        return _adapter.ExecuteAsync(request, cancellationToken);
    }

    private static bool ShouldFail(PipelineStageExecutionRequest request, string worker)
    {
        if (!request.Inputs.TryGetValue($"fail:{request.StageId}:{worker}", out var shouldFail))
            return false;

        if (bool.TryParse(shouldFail, out var parsed))
            return parsed;

        return string.Equals(shouldFail, "1", StringComparison.OrdinalIgnoreCase);
    }
}
