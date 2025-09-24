using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure.DataStore
{
    /// <summary>
    /// Data store metrics
    /// </summary>
    public record DataStoreMetrics
    {
        /// <summary>
        /// Metrics identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Metrics timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Connection count
        /// </summary>
        public int ConnectionCount { get; init; }
        
        /// <summary>
        /// Active connection count
        /// </summary>
        public int ActiveConnectionCount { get; init; }
        
        /// <summary>
        /// Query count
        /// </summary>
        public long QueryCount { get; init; }
        
        /// <summary>
        /// Average query time
        /// </summary>
        public TimeSpan AverageQueryTime { get; init; }
        
        /// <summary>
        /// Error count
        /// </summary>
        public long ErrorCount { get; init; }
        
        /// <summary>
        /// Error rate
        /// </summary>
        public double ErrorRate { get; init; }
        
        /// <summary>
        /// Throughput (queries per second)
        /// </summary>
        public double Throughput { get; init; }
        
        /// <summary>
        /// Metrics metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store performance metrics
    /// </summary>
    public record DataStorePerformanceMetrics
    {
        /// <summary>
        /// Performance metrics identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Metrics timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsage { get; init; }
        
        /// <summary>
        /// Memory usage percentage
        /// </summary>
        public double MemoryUsage { get; init; }
        
        /// <summary>
        /// Disk usage percentage
        /// </summary>
        public double DiskUsage { get; init; }
        
        /// <summary>
        /// Network usage percentage
        /// </summary>
        public double NetworkUsage { get; init; }
        
        /// <summary>
        /// Cache hit rate
        /// </summary>
        public double CacheHitRate { get; init; }
        
        /// <summary>
        /// Index usage percentage
        /// </summary>
        public double IndexUsage { get; init; }
        
        /// <summary>
        /// Performance metrics metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store health metrics
    /// </summary>
    public record DataStoreHealthMetrics
    {
        /// <summary>
        /// Health metrics identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Metrics timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Overall health status
        /// </summary>
        public string HealthStatus { get; init; } = "Unknown";
        
        /// <summary>
        /// Health score (0-100)
        /// </summary>
        public double HealthScore { get; init; }
        
        /// <summary>
        /// Availability percentage
        /// </summary>
        public double Availability { get; init; }
        
        /// <summary>
        /// Response time
        /// </summary>
        public TimeSpan ResponseTime { get; init; }
        
        /// <summary>
        /// Error rate
        /// </summary>
        public double ErrorRate { get; init; }
        
        /// <summary>
        /// Warning count
        /// </summary>
        public int WarningCount { get; init; }
        
        /// <summary>
        /// Critical count
        /// </summary>
        public int CriticalCount { get; init; }
        
        /// <summary>
        /// Health metrics metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store capacity metrics
    /// </summary>
    public record DataStoreCapacityMetrics
    {
        /// <summary>
        /// Capacity metrics identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Metrics timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Total storage capacity
        /// </summary>
        public long TotalStorageCapacity { get; init; }
        
        /// <summary>
        /// Used storage capacity
        /// </summary>
        public long UsedStorageCapacity { get; init; }
        
        /// <summary>
        /// Available storage capacity
        /// </summary>
        public long AvailableStorageCapacity { get; init; }
        
        /// <summary>
        /// Storage usage percentage
        /// </summary>
        public double StorageUsagePercentage { get; init; }
        
        /// <summary>
        /// Maximum connection limit
        /// </summary>
        public int MaxConnectionLimit { get; init; }
        
        /// <summary>
        /// Current connection count
        /// </summary>
        public int CurrentConnectionCount { get; init; }
        
        /// <summary>
        /// Connection usage percentage
        /// </summary>
        public double ConnectionUsagePercentage { get; init; }
        
        /// <summary>
        /// Capacity metrics metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store metrics summary
    /// </summary>
    public record DataStoreMetricsSummary
    {
        /// <summary>
        /// Summary identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Summary timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Time range covered
        /// </summary>
        public TimeSpan TimeRange { get; init; }
        
        /// <summary>
        /// Basic metrics
        /// </summary>
        public DataStoreMetrics BasicMetrics { get; init; } = new();
        
        /// <summary>
        /// Performance metrics
        /// </summary>
        public DataStorePerformanceMetrics PerformanceMetrics { get; init; } = new();
        
        /// <summary>
        /// Health metrics
        /// </summary>
        public DataStoreHealthMetrics HealthMetrics { get; init; } = new();
        
        /// <summary>
        /// Capacity metrics
        /// </summary>
        public DataStoreCapacityMetrics CapacityMetrics { get; init; } = new();
        
        /// <summary>
        /// Summary metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
