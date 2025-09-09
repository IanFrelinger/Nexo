using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for storing performance data
/// </summary>
public interface IPerformanceDataStore
{
    /// <summary>
    /// Store performance data
    /// </summary>
    Task StorePerformanceDataAsync(PerformanceData data);
    
    /// <summary>
    /// Store multiple performance data points
    /// </summary>
    Task StorePerformanceDataAsync(IEnumerable<PerformanceData> data);
    
    /// <summary>
    /// Get performance data by ID
    /// </summary>
    Task<PerformanceData?> GetPerformanceDataAsync(string id);
    
    /// <summary>
    /// Get performance data within a time range
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetPerformanceDataAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get performance data by metric name
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetPerformanceDataByMetricAsync(string metricName, DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get latest performance data for a metric
    /// </summary>
    Task<PerformanceData?> GetLatestPerformanceDataAsync(string metricName);
    
    /// <summary>
    /// Get performance data summary
    /// </summary>
    Task<PerformanceDataSummary> GetPerformanceDataSummaryAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Delete old performance data
    /// </summary>
    Task DeleteOldPerformanceDataAsync(DateTime cutoffTime);
    
    /// <summary>
    /// Get recent performance data
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetRecentPerformanceDataAsync(TimeSpan timeSpan);
}
