using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Interface for aggregating metrics
/// </summary>
public interface IMetricsAggregator
{
    /// <summary>
    /// Aggregate metrics
    /// </summary>
    Task<MetricsAggregation> AggregateMetricsAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get aggregated metrics
    /// </summary>
    Task<MetricsAggregation> GetAggregatedMetricsAsync();
    
    /// <summary>
    /// Get metrics summary
    /// </summary>
    Task<MetricsSummary> GetMetricsSummaryAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get metrics trends
    /// </summary>
    Task<IEnumerable<MetricsTrend>> GetMetricsTrendsAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get metrics insights
    /// </summary>
    Task<IEnumerable<LearningInsight>> GetMetricsInsightsAsync();
    
    /// <summary>
    /// Get metrics recommendations
    /// </summary>
    Task<IEnumerable<string>> GetMetricsRecommendationsAsync();
    
    /// <summary>
    /// Clear old metrics
    /// </summary>
    Task ClearOldMetricsAsync(DateTime cutoffTime);
    
    /// <summary>
    /// Get metrics statistics
    /// </summary>
    Task<MetricsStatistics> GetMetricsStatisticsAsync();
}
