using System;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Performance requirements for operations
/// </summary>
public record PerformanceRequirements
{
    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public int MaxExecutionTimeMs { get; init; } = 5000;
    
    /// <summary>
    /// Maximum memory usage in MB
    /// </summary>
    public long MaxMemoryUsageMB { get; init; } = 100;
    
    /// <summary>
    /// Maximum CPU usage percentage
    /// </summary>
    public double MaxCpuUsagePercentage { get; init; } = 80.0;
    
    /// <summary>
    /// Whether real-time performance is required
    /// </summary>
    public bool RequiresRealTime { get; init; } = false;
    
    /// <summary>
    /// Whether memory usage is critical
    /// </summary>
    public bool MemoryCritical { get; init; } = false;
    
    /// <summary>
    /// Whether parallel execution is preferred
    /// </summary>
    public bool PreferParallel { get; init; } = false;
    
    /// <summary>
    /// Whether low allocation is required
    /// </summary>
    public bool LowAllocation { get; init; } = false;
    
    /// <summary>
    /// Whether deterministic order is required
    /// </summary>
    public bool DeterministicOrder { get; init; } = false;
    
    /// <summary>
    /// Target throughput (operations per second)
    /// </summary>
    public double TargetThroughput { get; init; } = 1000.0;
    
    /// <summary>
    /// Target latency in milliseconds
    /// </summary>
    public double TargetLatencyMs { get; init; } = 100.0;
    
    /// <summary>
    /// Additional performance constraints
    /// </summary>
    public Dictionary<string, object> Constraints { get; init; } = new();
}
