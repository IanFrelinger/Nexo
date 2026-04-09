using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.Execution.Routing;

/// <summary>
/// NCR-driven capability router for local vs RunPod execution.
/// </summary>
public sealed class NcrCapabilityRouter : ICapabilityRouter
{
    private readonly INCRCapabilitySnapshot _snapshot;
    private readonly ILocalExecutor _localExecutor;
    private readonly RunPodBrick _runPodBrick;
    private readonly IOptions<RunPodBrickConfig> _config;
    private readonly ILogger<NcrCapabilityRouter> _logger;

    public NcrCapabilityRouter(
        INCRCapabilitySnapshot snapshot,
        ILocalExecutor localExecutor,
        RunPodBrick runPodBrick,
        IOptions<RunPodBrickConfig> config,
        ILogger<NcrCapabilityRouter> logger)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _localExecutor = localExecutor ?? throw new ArgumentNullException(nameof(localExecutor));
        _runPodBrick = runPodBrick ?? throw new ArgumentNullException(nameof(runPodBrick));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ExecutionTarget ResolveExecutionTarget(JobRequirements requirements)
    {
        if (requirements.IsOvernightOrBackground)
        {
            const string reason = "Overnight/background job forces remote execution.";
            _logger.LogInformation("capability-routing decision=remote reason={Reason}", reason);
            return new ExecutionTarget.Remote(_runPodBrick, reason);
        }

        if (_snapshot.AvailableVramBytes < requirements.MinimumVramBytes)
        {
            var reason = $"Insufficient VRAM: available={_snapshot.AvailableVramBytes}, required={requirements.MinimumVramBytes}.";
            _logger.LogInformation("capability-routing decision=remote reason={Reason}", reason);
            return new ExecutionTarget.Remote(_runPodBrick, reason);
        }

        if (_snapshot.ComputeClass < requirements.ComputeClass)
        {
            var reason = $"Insufficient compute class: available={_snapshot.ComputeClass}, required={requirements.ComputeClass}.";
            _logger.LogInformation("capability-routing decision=remote reason={Reason}", reason);
            return new ExecutionTarget.Remote(_runPodBrick, reason);
        }

        var threshold = Math.Max(0, _config.Value.QueueDepthThreshold);
        if (_snapshot.CurrentQueueDepth > threshold)
        {
            var reason = $"Local queue depth threshold exceeded: depth={_snapshot.CurrentQueueDepth}, threshold={threshold}.";
            _logger.LogInformation("capability-routing decision=remote reason={Reason}", reason);
            return new ExecutionTarget.Remote(_runPodBrick, reason);
        }

        const string localReason = "Local capabilities satisfy VRAM, compute class, and queue-depth checks.";
        _logger.LogInformation("capability-routing decision=local reason={Reason}", localReason);
        return new ExecutionTarget.Local(_localExecutor, localReason);
    }
}
