using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Pipelines.Models;

namespace Nexo.Infrastructure.Pipelines;

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

        _logger.LogInformation(
            "Agentic adapter '{AdapterKey}' executed stage {StageId}.",
            AdapterKey,
            request.StageId);

        return Task.FromResult(new PipelineStageExecutionResult
        {
            Succeeded = true,
            Retryable = false,
            WorkerId = "agentic-default",
            Output = $"agentic:{request.StageId}:ok"
        });
    }
}
