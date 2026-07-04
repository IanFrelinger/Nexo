using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.BackgroundAgents.Scheduling;

/// <summary>
/// Executes a single background agent's schedule (continuous, interval, or cron).
/// Used by AgentScheduler to run each agent's execution loop.
/// </summary>
public interface IScheduleExecutor
{
    /// <summary>
    /// Runs the agent's schedule loop until cancellation.
    /// </summary>
    /// <param name="instance">The agent instance to run.</param>
    /// <param name="executeOnce">Delegate to run one execution tick.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(
        BackgroundAgentInstance instance,
        Func<BackgroundAgentInstance, CancellationToken, Task> executeOnce,
        CancellationToken cancellationToken = default);
}
