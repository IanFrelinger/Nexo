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

/// <summary>
/// Implementation of IScheduleExecutor.
/// </summary>
public sealed class ScheduleExecutor : IScheduleExecutor
{
    private readonly ILogger<ScheduleExecutor>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleExecutor"/> class.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public ScheduleExecutor(ILogger<ScheduleExecutor>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(
        BackgroundAgentInstance instance,
        Func<BackgroundAgentInstance, CancellationToken, Task> executeOnce,
        CancellationToken cancellationToken = default)
    {
        if (instance?.Config == null)
            throw new ArgumentNullException(nameof(instance));
        if (executeOnce == null)
            throw new ArgumentNullException(nameof(executeOnce));

        if (instance.Config.Schedule.InitialDelay.HasValue)
        {
            await Task.Delay(instance.Config.Schedule.InitialDelay.Value, cancellationToken);
        }

        switch (instance.Config.Schedule.Type)
        {
            case ScheduleType.Continuous:
                await RunContinuousAsync(instance, executeOnce, cancellationToken);
                break;
            case ScheduleType.Interval:
                await RunIntervalAsync(instance, executeOnce, cancellationToken);
                break;
            case ScheduleType.Cron:
                await RunCronAsync(instance, executeOnce, cancellationToken);
                break;
            default:
                _logger?.LogWarning("Unknown schedule type {Type} for agent {AgentId}", instance.Config.Schedule.Type, instance.Config.Id);
                break;
        }
    }

    private async Task RunContinuousAsync(
        BackgroundAgentInstance instance,
        Func<BackgroundAgentInstance, CancellationToken, Task> executeOnce,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && instance.State == BackgroundAgentState.Running)
        {
            await executeOnce(instance, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private async Task RunIntervalAsync(
        BackgroundAgentInstance instance,
        Func<BackgroundAgentInstance, CancellationToken, Task> executeOnce,
        CancellationToken cancellationToken)
    {
        if (!instance.Config.Schedule.Interval.HasValue)
        {
            throw new InvalidOperationException($"Agent {instance.Config.Id} has interval schedule but no interval specified");
        }

        while (!cancellationToken.IsCancellationRequested && instance.State == BackgroundAgentState.Running)
        {
            await executeOnce(instance, cancellationToken);
            await Task.Delay(instance.Config.Schedule.Interval.Value, cancellationToken);
        }
    }

    private async Task RunCronAsync(
        BackgroundAgentInstance instance,
        Func<BackgroundAgentInstance, CancellationToken, Task> executeOnce,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(instance.Config.Schedule.CronExpression))
        {
            throw new InvalidOperationException($"Agent {instance.Config.Id} has cron schedule but no cron expression specified");
        }

        var schedule = NCrontab.CrontabSchedule.Parse(instance.Config.Schedule.CronExpression);

        while (!cancellationToken.IsCancellationRequested && instance.State == BackgroundAgentState.Running)
        {
            var nextRun = schedule.GetNextOccurrence(DateTime.UtcNow);
            var delay = nextRun - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await executeOnce(instance, cancellationToken);
            }
        }
    }
}
