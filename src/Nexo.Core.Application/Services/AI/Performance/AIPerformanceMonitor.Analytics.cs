using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Performance
{
    /// <summary>
    /// Performance analytics and statistics functionality for AIPerformanceMonitor.
    /// </summary>
    public partial class AIPerformanceMonitor
    {
        /// <summary>
        /// Gets historical performance metrics with optional filtering.
        /// </summary>
        private async Task<List<PerformanceMetrics>> GetHistoricalMetricsInternalAsync(TimeSpan? timeRange = null, AIOperationType? operationType = null, AIProviderType? providerType = null)
        {
            try
            {
                var cutoffTime = timeRange.HasValue ? DateTime.UtcNow - timeRange.Value : DateTime.MinValue;
                
                lock (_lockObject)
                {
                    var filteredMetrics = new List<PerformanceMetrics>();
                    
                    foreach (var metrics in _historicalMetrics)
                    {
                        if (metrics.StartTime < cutoffTime)
                            continue;
                            
                        if (operationType.HasValue && metrics.OperationType != operationType.Value)
                            continue;
                            
                        if (providerType.HasValue && metrics.ProviderType != providerType.Value)
                            continue;
                            
                        filteredMetrics.Add(metrics);
                    }
                    
                    return await Task.FromResult(filteredMetrics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get historical metrics");
                throw;
            }
        }

        /// <summary>
        /// Calculates performance statistics for AI operations.
        /// </summary>
        private async Task<PerformanceStatistics> GetPerformanceStatisticsInternalAsync(TimeSpan? timeRange = null)
        {
            try
            {
                _logger.LogInformation("Calculating performance statistics");

                var metrics = await GetHistoricalMetricsInternalAsync(timeRange);
                
                if (metrics.Count == 0)
                {
                    return new PerformanceStatistics
                    {
                        TotalOperations = 0,
                        AverageDuration = TimeSpan.Zero,
                        AveragePerformanceScore = 0,
                        SuccessRate = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                var statistics = new PerformanceStatistics
                {
                    TotalOperations = metrics.Count,
                    SuccessfulOperations = metrics.Count(m => m.Status == AIOperationStatus.Completed),
                    FailedOperations = metrics.Count(m => m.Status == AIOperationStatus.Failed),
                    AverageDuration = TimeSpan.FromMilliseconds(metrics.Average(m => m.Duration.TotalMilliseconds)),
                    MinDuration = metrics.Min(m => m.Duration),
                    MaxDuration = metrics.Max(m => m.Duration),
                    AveragePerformanceScore = metrics.Average(m => m.PerformanceScore),
                    AverageMemoryUsage = metrics.Average(m => m.FinalMemoryUsage),
                    AverageCpuUsage = metrics.Average(m => m.FinalCpuUsage),
                    LastUpdated = DateTime.UtcNow
                };

                // Calculate success rate
                statistics.SuccessRate = (double)statistics.SuccessfulOperations / statistics.TotalOperations * 100;

                // Calculate performance trends
                statistics.PerformanceTrend = CalculatePerformanceTrend(metrics);

                _logger.LogInformation("Performance statistics calculated: {TotalOperations} operations, {SuccessRate}% success rate, {AverageDuration}ms average duration", 
                    statistics.TotalOperations, statistics.SuccessRate, statistics.AverageDuration.TotalMilliseconds);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate performance statistics");
                throw;
            }
        }

        /// <summary>
        /// Generates performance recommendations based on current metrics.
        /// </summary>
        private async Task<List<PerformanceRecommendation>> GetPerformanceRecommendationsInternalAsync()
        {
            try
            {
                _logger.LogInformation("Generating performance recommendations");

                var recommendations = new List<PerformanceRecommendation>();
                var statistics = await GetPerformanceStatisticsInternalAsync();

                // Check for performance issues
                if (statistics.AveragePerformanceScore < 70)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Performance Score",
                        Priority = "High",
                        Message = "Average performance score is below 70. Consider optimizing AI operations.",
                        Recommendation = "Review and optimize AI model configurations, increase memory allocation, or consider using a different AI provider.",
                        Impact = "High"
                    });
                }

                if (statistics.SuccessRate < 95)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Success Rate",
                        Priority = "High",
                        Message = $"Success rate is {statistics.SuccessRate:F1}%. Consider investigating failures.",
                        Recommendation = "Review error logs, check AI provider availability, and implement better error handling.",
                        Impact = "High"
                    });
                }

                if (statistics.AverageDuration.TotalSeconds > 30)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Response Time",
                        Priority = "Medium",
                        Message = $"Average response time is {statistics.AverageDuration.TotalSeconds:F1} seconds. Consider optimization.",
                        Recommendation = "Use smaller models, implement caching, or consider using faster AI providers.",
                        Impact = "Medium"
                    });
                }

                if (statistics.AverageMemoryUsage > 1024 * 1024 * 1024) // 1GB
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Type = "Memory Usage",
                        Priority = "Medium",
                        Message = "High memory usage detected. Consider memory optimization.",
                        Recommendation = "Use quantized models, implement memory pooling, or consider using smaller models.",
                        Impact = "Medium"
                    });
                }

                _logger.LogInformation("Generated {Count} performance recommendations", recommendations.Count);
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate performance recommendations");
                throw;
            }
        }
    }
}
