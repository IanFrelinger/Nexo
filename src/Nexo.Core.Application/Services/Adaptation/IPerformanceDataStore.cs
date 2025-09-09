using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

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
    /// Get performance data by ID
    /// </summary>
    Task<PerformanceData?> GetPerformanceDataAsync(string id);
    
    /// <summary>
    /// Get performance data within time range
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetPerformanceDataAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get recent performance data
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetRecentPerformanceDataAsync(int count = 10);
    
    /// <summary>
    /// Get performance summary
    /// </summary>
    Task<PerformanceDataSummary> GetPerformanceSummaryAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Delete old performance data
    /// </summary>
    Task DeleteOldPerformanceDataAsync(DateTime cutoffTime);
}
