using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Pipelines.Models;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Default deterministic adapter placeholder.
/// Replace with concrete deterministic engine integration in environment-specific composition.
/// </summary>
public sealed class DefaultDeterministicStageExecutionAdapter : IPipelineStageExecutionAdapter
{
    private readonly ILogger<DefaultDeterministicStageExecutionAdapter> _logger;

    public DefaultDeterministicStageExecutionAdapter(ILogger<DefaultDeterministicStageExecutionAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string AdapterKey => "default";

    public PipelineWorkerType WorkerType => PipelineWorkerType.Deterministic;

    public Task<PipelineStageExecutionResult> ExecuteAsync(
        PipelineStageExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null) throw new ArgumentNullException(nameof(request));

        _logger.LogInformation(
            "Deterministic adapter '{AdapterKey}' executed stage {StageId}.",
            AdapterKey,
            request.StageId);

        return Task.FromResult(new PipelineStageExecutionResult
        {
            Succeeded = true,
            Retryable = false,
            WorkerId = "deterministic-default",
            Output = $"deterministic:{request.StageId}:ok"
        });
    }
}
