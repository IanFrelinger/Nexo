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

    public DefaultAgenticStageExecutionAdapter(ILogger<DefaultAgenticStageExecutionAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string AdapterKey => "default";

    public PipelineWorkerType WorkerType => PipelineWorkerType.Agentic;

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
