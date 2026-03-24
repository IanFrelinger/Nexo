using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Executes agentic stages.
/// </summary>
public sealed class AgenticPipelineStageExecutor : IPipelineStageExecutor
{
    private readonly ILogger<AgenticPipelineStageExecutor> _logger;

    public AgenticPipelineStageExecutor(ILogger<AgenticPipelineStageExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public PipelineWorkerType WorkerType => PipelineWorkerType.Agentic;

    public Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Test hook to force agentic stage failures.
        var fail = ShouldFail(request, "agentic");
        if (fail)
        {
            _logger.LogWarning("Agentic stage {StageId} failed by test hook.", request.StageId);
            return Task.FromResult(new PipelineStageExecutionResult
            {
                Succeeded = false,
                Retryable = true,
                WorkerId = "agentic-default",
                Error = "Agentic execution failed (test hook)."
            });
        }

        _logger.LogInformation("Agentic stage {StageId} completed.", request.StageId);
        return Task.FromResult(new PipelineStageExecutionResult
        {
            Succeeded = true,
            Retryable = false,
            WorkerId = "agentic-default",
            Output = $"agentic:{request.StageId}:ok"
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
