using System;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Metric data point for monitoring and analysis
/// </summary>
public record MetricDataPoint
{
    /// <summary>
    /// Data point identifier
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
    /// Metric unit
    /// </summary>
    public string Unit { get; init; } = string.Empty;
    
    /// <summary>
    /// Data point timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data point source
    /// </summary>
    public string Source { get; init; } = string.Empty;
    
    /// <summary>
    /// Data point context
    /// </summary>
    public string Context { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Data quality score (0-1)
    /// </summary>
    public double QualityScore { get; init; } = 1.0;
    
    /// <summary>
    /// Whether this is an anomaly
    /// </summary>
    public bool IsAnomaly { get; init; } = false;
    
    /// <summary>
    /// Anomaly score (0-1) if applicable
    /// </summary>
    public double AnomalyScore { get; init; } = 0.0;
}