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
