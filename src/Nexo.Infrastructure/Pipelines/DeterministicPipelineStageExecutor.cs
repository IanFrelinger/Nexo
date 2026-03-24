using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Executes deterministic stages.
/// </summary>
public sealed class DeterministicPipelineStageExecutor : IPipelineStageExecutor
{
    private readonly ILogger<DeterministicPipelineStageExecutor> _logger;

    public DeterministicPipelineStageExecutor(ILogger<DeterministicPipelineStageExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public PipelineWorkerType WorkerType => PipelineWorkerType.Deterministic;

    public Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Test hook to force deterministic stage failures.
        var fail = ShouldFail(request, "deterministic");
        if (fail)
        {
            _logger.LogWarning("Deterministic stage {StageId} failed by test hook.", request.StageId);
            return Task.FromResult(new PipelineStageExecutionResult
            {
                Succeeded = false,
                Retryable = true,
                WorkerId = "deterministic-default",
                Error = "Deterministic execution failed (test hook)."
            });
        }

        _logger.LogInformation("Deterministic stage {StageId} completed.", request.StageId);
        return Task.FromResult(new PipelineStageExecutionResult
        {
            Succeeded = true,
            Retryable = false,
            WorkerId = "deterministic-default",
            Output = $"deterministic:{request.StageId}:ok"
        });
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
