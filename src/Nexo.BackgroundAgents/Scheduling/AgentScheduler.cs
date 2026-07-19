using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.BackgroundAgents.Scheduling;

/// <summary>
/// Implementation of IAgentScheduler.
/// </summary>
public sealed class AgentScheduler : IAgentScheduler
{
    private readonly IScheduleExecutor _executor;
    private readonly ConcurrentDictionary<string, (CancellationTokenSource Cts, Task Task)> _running = new();
    private readonly ILogger<AgentScheduler>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentScheduler"/> class.
    /// </summary>
    /// <param name="executor">Schedule executor.</param>
    /// <param name="logger">Optional logger.</param>
    public AgentScheduler(IScheduleExecutor executor, ILogger<AgentScheduler>? logger = null)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(
        BackgroundAgentInstance instance,
        Func<BackgroundAgentInstance, CancellationToken, Task> executeOnce,
        CancellationToken cancellationToken = default)
    {
        if (instance?.Config == null)
            throw new ArgumentNullException(nameof(instance));
        if (executeOnce == null)
            throw new ArgumentNullException(nameof(executeOnce));

        var agentId = instance.Config.Id;

        if (_running.ContainsKey(agentId))
        {
            _logger?.LogWarning("Agent {AgentId} is already scheduled", agentId);
            return Task.CompletedTask;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // Never execute a schedule inline on IHostedService.StartAsync. Inline
        // execution can block host startup when a schedule has no initial await,
        // and previously raced instance.State while it was still Starting.
        var task = Task.Run(
            () => _executor.ExecuteAsync(instance, executeOnce, cts.Token),
            CancellationToken.None);

        _running[agentId] = (cts, task);
        _logger?.LogInformation("Started schedule for agent: {AgentId}", agentId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Stop(string agentId)
    {
        if (_running.TryRemove(agentId, out var run))
        {
            try
            {
                run.Cts.Cancel();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error cancelling schedule for agent {AgentId}", agentId);
            }

            _logger?.LogInformation("Stopped schedule for agent: {AgentId}", agentId);
        }
    }
}
