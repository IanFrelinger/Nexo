using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for aggregating metrics
/// </summary>
public interface IMetricsAggregator
{
    /// <summary>
    /// Aggregate performance metrics
    /// </summary>
    Task<PerformanceMetrics> AggregatePerformanceMetricsAsync(IEnumerable<PerformanceData> data);
    
    /// <summary>
    /// Aggregate metrics by time window
    /// </summary>
    Task<Dictionary<DateTime, PerformanceMetrics>> AggregateMetricsByTimeWindowAsync(
        IEnumerable<PerformanceData> data, 
        TimeSpan windowSize);
    
    /// <summary>
    /// Calculate metric trends
    /// </summary>
    Task<MetricTrend> CalculateTrendAsync(string metricName, IEnumerable<PerformanceData> data);
    
    /// <summary>
    /// Get metric statistics
    /// </summary>
    Task<MetricStatistics> GetMetricStatisticsAsync(string metricName, IEnumerable<PerformanceData> data);
    
    /// <summary>
    /// Detect anomalies in metrics
    /// </summary>
    Task<IEnumerable<MetricAnomaly>> DetectAnomaliesAsync(IEnumerable<PerformanceData> data);
}

/// <summary>
/// Metric trend information
/// </summary>
public record MetricTrend
{
    /// <summary>
    /// Trend direction
    /// </summary>
    public TrendDirection Direction { get; init; }
    
    /// <summary>
    /// Trend strength (0-1)
    /// </summary>
    public double Strength { get; init; }
    
    /// <summary>
    /// Rate of change
    /// </summary>
    public double RateOfChange { get; init; }
    
    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Trend directions
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Increasing trend
    /// </summary>
    Increasing,
    
    /// <summary>
    /// Decreasing trend
    /// </summary>
    Decreasing,
    
    /// <summary>
    /// Stable trend
    /// </summary>
    Stable,
    
    /// <summary>
    /// Volatile trend
    /// </summary>
    Volatile
}

/// <summary>
/// Metric statistics
/// </summary>
public record MetricStatistics
{
    /// <summary>
    /// Mean value
    /// </summary>
    public double Mean { get; init; }
    
    /// <summary>
    /// Median value
    /// </summary>
    public double Median { get; init; }
    
    /// <summary>
    /// Standard deviation
    /// </summary>
    public double StandardDeviation { get; init; }
    
    /// <summary>
    /// Minimum value
    /// </summary>
    public double Minimum { get; init; }
    
    /// <summary>
    /// Maximum value
    /// </summary>
    public double Maximum { get; init; }
    
    /// <summary>
    /// 95th percentile
    /// </summary>
    public double Percentile95 { get; init; }
    
    /// <summary>
    /// Number of data points
    /// </summary>
    public int DataPointCount { get; init; }
}

/// <summary>
/// Metric anomaly information
/// </summary>
public record MetricAnomaly
{
    /// <summary>
    /// Anomaly identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Metric name
    /// </summary>
    public string MetricName { get; init; } = string.Empty;
    
    /// <summary>
    /// Anomaly value
    /// </summary>
    public double Value { get; init; }
    
    /// <summary>
    /// Expected value
    /// </summary>
    public double ExpectedValue { get; init; }
    
    /// <summary>
    /// Anomaly severity
    /// </summary>
    public AnomalySeverity Severity { get; init; }
    
    /// <summary>
    /// Timestamp when anomaly occurred
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Anomaly score (0-1)
    /// </summary>
    public double Score { get; init; }
}

/// <summary>
/// Anomaly severity levels
/// </summary>
public enum AnomalySeverity
{
    /// <summary>
    /// Low severity
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium severity
    /// </summary>
    Medium,
    
    /// <summary>
    /// High severity
    /// </summary>
    High,
    
    /// <summary>
    /// Critical severity
    /// </summary>
    Critical
}
