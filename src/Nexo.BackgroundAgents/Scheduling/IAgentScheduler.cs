using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.BackgroundAgents.Scheduling;

/// <summary>
/// Schedules and runs background agent execution loops.
/// Uses ScheduleExecutor per agent and tracks running tasks for stop support.
/// </summary>
public interface IAgentScheduler
{
    /// <summary>
    /// Start the schedule loop for an agent.
    /// </summary>
    /// <param name="instance">The agent instance to run.</param>
    /// <param name="executeOnce">Delegate to run one execution tick.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(
        BackgroundAgentInstance instance,
        Func<BackgroundAgentInstance, CancellationToken, Task> executeOnce,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the schedule loop for an agent (signals cancellation; caller should set instance state).
    /// </summary>
    /// <param name="agentId">The agent id to stop.</param>
    void Stop(string agentId);
}
