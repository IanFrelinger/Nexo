using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for performance profiling
/// </summary>
public interface IPerformanceProfiler
{
    /// <summary>
    /// Start profiling a method or operation
    /// </summary>
    Task<IProfilingSession> StartProfilingAsync(string operationName);
    
    /// <summary>
    /// Stop profiling performance metrics
    /// </summary>
    Task StopProfilingAsync();
    
    /// <summary>
    /// Get current performance metrics
    /// </summary>
    Task<PerformanceMetrics> GetCurrentMetricsAsync();
    
    /// <summary>
    /// Record a performance data point
    /// </summary>
    Task RecordPerformanceDataAsync(PerformanceData data);
    
    /// <summary>
    /// Get performance data within a time range
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetPerformanceDataAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get performance trends
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetPerformanceTrendsAsync(string metricName, TimeSpan timeWindow);
    
    /// <summary>
    /// Check if performance thresholds are exceeded
    /// </summary>
    Task<bool> AreThresholdsExceededAsync();
    
    /// <summary>
    /// Get performance alerts
    /// </summary>
    Task<IEnumerable<PerformanceAlert>> GetPerformanceAlertsAsync();
}

/// <summary>
/// Interface for profiling sessions
/// </summary>
public interface IProfilingSession : IDisposable
{
    /// <summary>
    /// Operation name being profiled
    /// </summary>
    string OperationName { get; }
    
    /// <summary>
    /// Start time of profiling
    /// </summary>
    DateTime StartTime { get; }
    
    /// <summary>
    /// End profiling and get results
    /// </summary>
    Task<ProfilingResult> EndProfilingAsync();
}

/// <summary>
/// Profiling result
/// </summary>
public record ProfilingResult
{
    /// <summary>
    /// Operation name
    /// </summary>
    public string OperationName { get; init; } = string.Empty;
    
    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    public double ExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; init; }
    
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercentage { get; init; }
    
    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// End time
    /// </summary>
    public DateTime EndTime { get; init; }
    
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool WasSuccessful { get; init; } = true;
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Profiling result with return value
/// </summary>
public record ProfilingResult<T> : ProfilingResult
{
    /// <summary>
    /// Return value from the operation
    /// </summary>
    public T? ReturnValue { get; init; }
}

/// <summary>
/// Performance summary
/// </summary>
public record PerformanceSummary
{
    /// <summary>
    /// Total number of operations profiled
    /// </summary>
    public int TotalOperations { get; init; }
    
    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public double MaximumExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Minimum execution time in milliseconds
    /// </summary>
    public double MinimumExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Average memory usage in bytes
    /// </summary>
    public long AverageMemoryUsageBytes { get; init; }
    
    /// <summary>
    /// Average CPU usage percentage
    /// </summary>
    public double AverageCpuUsagePercentage { get; init; }
    
    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRatePercentage { get; init; }
    
    /// <summary>
    /// Time window covered
    /// </summary>
    public TimeSpan TimeWindow { get; init; }
}