using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Manages timeouts for agent execution.
/// 
/// Responsibilities:
/// - Creates timeout cancellation tokens for agents
/// - Extracts timeout from agent specifications
/// - Cancels timeouts when execution completes
/// - Provides default timeout values
/// 
/// Used by Orchestrator to enforce execution time limits.
/// Prevents agents from running indefinitely.
/// </summary>
public sealed class TimeoutManager
{
    private readonly ILogger<TimeoutManager> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _timeouts = new();
    private readonly TimeSpan _defaultTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="defaultTimeout">Optional default timeout. If not provided, defaults to 30 minutes.</param>
    public TimeoutManager(ILogger<TimeoutManager> logger, TimeSpan? defaultTimeout = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Creates a timeout cancellation token for an agent execution.
    /// 
    /// The timeout is determined by:
    /// 1. The provided timeout parameter (if specified)
    /// 2. The agent's resource requirements (2x estimated compute time)
    /// 3. The default timeout (30 minutes)
    /// </summary>
    /// <param name="container">The agent container to create a timeout for.</param>
    /// <param name="timeout">Optional explicit timeout. If not provided, uses agent spec or default.</param>
    /// <returns>A cancellation token that will be cancelled when the timeout expires.</returns>
    /// <exception cref="ArgumentNullException">Thrown if container is null.</exception>
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
    /// <param name="agentId">The ID of the agent whose timeout should be cancelled.</param>
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
    /// 
    /// Uses 2x the estimated compute time as the timeout, if available.
    /// </summary>
    /// <param name="spec">The agent spawn specification.</param>
    /// <returns>The timeout duration, or null if not specified in the spec.</returns>
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
    /// Cleans up all timeouts by cancelling and disposing all cancellation token sources.
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

