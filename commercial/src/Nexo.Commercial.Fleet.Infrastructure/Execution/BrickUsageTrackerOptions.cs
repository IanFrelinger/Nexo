using Nexo.Core.Domain;

namespace Nexo.Commercial.Fleet.Infrastructure.Execution;
/// <summary>
/// Configuration for <see cref="BrickUsageTracker"/> rolling window and aggregation.
/// </summary>
public class BrickUsageTrackerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "BrickUsageTracker";

    /// <summary>Maximum number of execution records to keep (rolling window). Default 10,000.</summary>
    public int MaxEntries { get; set; } = NexoDefaults.BrickUsageTrackerMaxEntries;

    /// <summary>Rolling window for ExecutionsPerHour (seconds). Default 3600 (1 hour).</summary>
    public int RollingHourWindowSeconds { get; set; } = NexoDefaults.BrickUsageTrackerRollingHourWindowSeconds;
}
