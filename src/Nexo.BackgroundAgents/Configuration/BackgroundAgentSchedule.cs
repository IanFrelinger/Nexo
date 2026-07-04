namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Schedule configuration for background agent execution.
/// </summary>
public class BackgroundAgentSchedule
{
    /// <summary>
    /// Schedule type: "continuous", "interval", or "cron".
    /// </summary>
    public ScheduleType Type { get; set; } = ScheduleType.Interval;

    /// <summary>
    /// Interval for interval type (e.g., "00:05:00" for 5 minutes).
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Cron expression for cron type (e.g., "0 */6 * * *" for every 6 hours).
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Initial delay before first execution.
    /// </summary>
    public TimeSpan? InitialDelay { get; set; }
}
