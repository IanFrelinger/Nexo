namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Schedule type enumeration.
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// Run continuously (think loop).
    /// </summary>
    Continuous,

    /// <summary>
    /// Run at fixed intervals.
    /// </summary>
    Interval,

    /// <summary>
    /// Run on cron schedule.
    /// </summary>
    Cron
}
