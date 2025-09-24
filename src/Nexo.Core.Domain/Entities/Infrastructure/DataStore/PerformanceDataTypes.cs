using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure.DataStore
{
    /// <summary>
    /// Performance data summary
    /// </summary>
    public record PerformanceDataSummary
    {
        /// <summary>
        /// Total number of data points
        /// </summary>
        public int TotalDataPoints { get; init; }
        
        /// <summary>
        /// Time range covered
        /// </summary>
        public TimeSpan TimeRange { get; init; }
        
        /// <summary>
        /// Average values by metric
        /// </summary>
        public Dictionary<string, double> AverageValues { get; init; } = new();
        
        /// <summary>
        /// Maximum values by metric
        /// </summary>
        public Dictionary<string, double> MaximumValues { get; init; } = new();
        
        /// <summary>
        /// Minimum values by metric
        /// </summary>
        public Dictionary<string, double> MinimumValues { get; init; } = new();
        
        /// <summary>
        /// Unique metrics recorded
        /// </summary>
        public List<string> Metrics { get; init; } = new();
    }

    /// <summary>
    /// Performance data point
    /// </summary>
    public record PerformanceDataPoint
    {
        /// <summary>
        /// Timestamp of the data point
        /// </summary>
        public DateTime Timestamp { get; init; }
        
        /// <summary>
        /// Metric name
        /// </summary>
        public string MetricName { get; init; } = string.Empty;
        
        /// <summary>
        /// Metric value
        /// </summary>
        public double Value { get; init; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Performance data collection
    /// </summary>
    public record PerformanceDataCollection
    {
        /// <summary>
        /// Collection identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Collection name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Data points in the collection
        /// </summary>
        public List<PerformanceDataPoint> DataPoints { get; init; } = new();
        
        /// <summary>
        /// Collection metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        /// <summary>
        /// Collection summary
        /// </summary>
        public PerformanceDataSummary Summary { get; init; } = new();
    }

    /// <summary>
    /// Performance data query
    /// </summary>
    public record PerformanceDataQuery
    {
        /// <summary>
        /// Start time for the query
        /// </summary>
        public DateTime StartTime { get; init; }
        
        /// <summary>
        /// End time for the query
        /// </summary>
        public DateTime EndTime { get; init; }
        
        /// <summary>
        /// Metric names to include
        /// </summary>
        public List<string> MetricNames { get; init; } = new();
        
        /// <summary>
        /// Aggregation type
        /// </summary>
        public string AggregationType { get; init; } = "none";
        
        /// <summary>
        /// Grouping criteria
        /// </summary>
        public List<string> GroupBy { get; init; } = new();
        
        /// <summary>
        /// Filter criteria
        /// </summary>
        public Dictionary<string, object> Filters { get; init; } = new();
    }

    /// <summary>
    /// Performance data result
    /// </summary>
    public record PerformanceDataResult
    {
        /// <summary>
        /// Query that generated this result
        /// </summary>
        public PerformanceDataQuery Query { get; init; } = new();
        
        /// <summary>
        /// Data points matching the query
        /// </summary>
        public List<PerformanceDataPoint> DataPoints { get; init; } = new();
        
        /// <summary>
        /// Aggregated results
        /// </summary>
        public Dictionary<string, double> AggregatedResults { get; init; } = new();
        
        /// <summary>
        /// Result metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        /// <summary>
        /// Query execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; init; }
    }
}
