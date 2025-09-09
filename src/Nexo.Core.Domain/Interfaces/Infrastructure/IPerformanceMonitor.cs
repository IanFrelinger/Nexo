using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for performance monitoring
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Start monitoring performance metrics
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Stop monitoring performance metrics
    /// </summary>
    Task StopMonitoringAsync();
    
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
    
    /// <summary>
    /// Event fired when performance degradation is detected
    /// </summary>
    event EventHandler<PerformanceDegradationEventArgs> OnPerformanceDegradation;
}

/// <summary>
/// Performance alert information
/// </summary>
public record PerformanceAlert
{
    /// <summary>
    /// Alert identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Alert severity
    /// </summary>
    public AlertSeverity Severity { get; init; }
    
    /// <summary>
    /// Alert message
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Metric that triggered the alert
    /// </summary>
    public string MetricName { get; init; } = string.Empty;
    
    /// <summary>
    /// Threshold value
    /// </summary>
    public double Threshold { get; init; }
    
    /// <summary>
    /// Actual value that exceeded threshold
    /// </summary>
    public double ActualValue { get; init; }
    
    /// <summary>
    /// Timestamp when alert was triggered
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
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

/// <summary>
/// Performance degradation event arguments
/// </summary>
public class PerformanceDegradationEventArgs : EventArgs
{
    /// <summary>
    /// Performance metrics at time of degradation
    /// </summary>
    public PerformanceMetrics Metrics { get; }
    
    /// <summary>
    /// Severity of degradation
    /// </summary>
    public AlertSeverity Severity { get; }
    
    /// <summary>
    /// Description of the degradation
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Constructor
    /// </summary>
    public PerformanceDegradationEventArgs(PerformanceMetrics metrics, AlertSeverity severity, string description)
    {
        Metrics = metrics;
        Severity = severity;
        Description = description;
    }
}
