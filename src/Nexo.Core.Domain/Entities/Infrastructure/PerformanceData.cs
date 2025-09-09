using System;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Performance data for monitoring and analysis
/// </summary>
public record PerformanceData
{
    /// <summary>
    /// Unique identifier for the performance data
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Metric name
    /// </summary>
    public string MetricName { get; init; } = string.Empty;
    
    /// <summary>
    /// Metric value
    /// </summary>
    public double Value { get; init; }
    
    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; init; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the metric was recorded
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Context where the metric was measured
    /// </summary>
    public string Context { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Performance metrics for system monitoring
/// </summary>
public record PerformanceMetrics
{
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsage { get; init; }
    
    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public double MemoryUsage { get; init; }
    
    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    public double ExecutionTime { get; init; }
    
    /// <summary>
    /// Throughput (operations per second)
    /// </summary>
    public double Throughput { get; init; }
    
    /// <summary>
    /// Error rate percentage
    /// </summary>
    public double ErrorRate { get; init; }
    
    /// <summary>
    /// Timestamp when metrics were collected
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Network latency in milliseconds
    /// </summary>
    public double NetworkLatency { get; init; } = 0.0;
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public double ResponseTime { get; init; } = 0.0;
    
    /// <summary>
    /// Severity level
    /// </summary>
    public AlertSeverity Severity { get; init; } = AlertSeverity.Low;
    
    /// <summary>
    /// Whether optimization is required
    /// </summary>
    public bool RequiresOptimization { get; init; } = false;
    
    /// <summary>
    /// Overall performance score (0.0 to 1.0)
    /// </summary>
    public double OverallScore { get; init; } = 1.0;
    
    /// <summary>
    /// Whether there are iteration bottlenecks
    /// </summary>
    public bool HasIterationBottlenecks { get; init; } = false;
}
