using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Interface for monitoring system performance
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Start monitoring
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Stop monitoring
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Get current performance metrics
    /// </summary>
    Task<PerformanceMetrics> GetCurrentMetricsAsync();
    
    /// <summary>
    /// Get performance data within time range
    /// </summary>
    Task<IEnumerable<PerformanceData>> GetPerformanceDataAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get performance alerts
    /// </summary>
    Task<IEnumerable<PerformanceAlert>> GetPerformanceAlertsAsync();
    
    /// <summary>
    /// Check if performance is degraded
    /// </summary>
    Task<bool> IsPerformanceDegradedAsync();
    
    /// <summary>
    /// Get performance recommendations
    /// </summary>
    Task<IEnumerable<string>> GetPerformanceRecommendationsAsync();
    
    /// <summary>
    /// Event raised when performance degrades
    /// </summary>
    event EventHandler<PerformanceDegradationEventArgs> OnPerformanceDegradation;
    
    /// <summary>
    /// Get performance insights
    /// </summary>
    Task<IEnumerable<LearningInsight>> GetPerformanceInsightsAsync();
}
