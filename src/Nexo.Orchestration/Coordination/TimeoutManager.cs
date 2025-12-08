using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Manages timeouts for agent execution.
/// </summary>
public sealed class TimeoutManager
{
    private readonly ILogger<TimeoutManager> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _timeouts = new();
    private readonly TimeSpan _defaultTimeout;

    public TimeoutManager(ILogger<TimeoutManager> logger, TimeSpan? defaultTimeout = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Creates a timeout cancellation token for an agent execution.
    /// </summary>
    public CancellationToken CreateTimeoutToken(AgentContainer container, TimeSpan? timeout = null)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        var agentId = container.AgentId;
        var actualTimeout = timeout ?? GetTimeoutFromSpec(container.Agent.Spec) ?? _defaultTimeout;

        // Cancel any existing timeout
        if (_timeouts.TryGetValue(agentId, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        var cts = new CancellationTokenSource(actualTimeout);
        _timeouts[agentId] = cts;

        _logger.LogDebug("Created timeout token for agent {AgentId} with timeout {Timeout}",
            agentId, actualTimeout);

        return cts.Token;
    }

    /// <summary>
    /// Cancels the timeout for an agent (when execution completes).
    /// </summary>
    public void CancelTimeout(string agentId)
    {
        if (_timeouts.TryGetValue(agentId, out var cts))
        {
            _timeouts.Remove(agentId);
            cts.Cancel();
            cts.Dispose();
            _logger.LogDebug("Cancelled timeout for agent {AgentId}", agentId);
        }
    }

    /// <summary>
    /// Gets the timeout duration from an agent spec.
    /// </summary>
    private TimeSpan? GetTimeoutFromSpec(AgentSpawnSpec spec)
    {
        if (spec.ResourceRequirements?.EstimatedComputeSeconds != null)
        {
            // Use 2x estimated compute time as timeout
            return TimeSpan.FromSeconds(spec.ResourceRequirements.EstimatedComputeSeconds.Value * 2);
        }

        return null;
    }

    /// <summary>
    /// Cleans up all timeouts.
    /// </summary>
    public void Dispose()
    {
        foreach (var (agentId, cts) in _timeouts)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _timeouts.Clear();
    }
}

