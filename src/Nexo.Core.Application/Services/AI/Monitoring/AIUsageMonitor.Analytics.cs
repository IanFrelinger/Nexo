using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Monitoring
{
    /// <summary>
    /// AI usage analytics and statistics
    /// </summary>
    public partial class AIUsageMonitor
    {
        /// <summary>
        /// Gets usage statistics for a specific time period
        /// </summary>
        public async Task<AIUsageStatistics> GetUsageStatisticsAsync(TimeSpan? timeRange = null, string? userId = null)
        {
            try
            {
                _logger.LogDebug("Generating AI usage statistics");

                var cutoffTime = timeRange.HasValue ? DateTime.UtcNow - timeRange.Value : DateTime.MinValue;
                
                List<AIUsageEvent> relevantEvents;
                lock (_lockObject)
                {
                    relevantEvents = _usageHistory
                        .Where(e => e.Timestamp >= cutoffTime)
                        .Where(e => userId == null || e.UserId == userId)
                        .ToList();
                }

                var statistics = new AIUsageStatistics
                {
                    TimeRange = timeRange,
                    UserId = userId,
                    TotalEvents = relevantEvents.Count,
                    TotalOperations = relevantEvents.Count(e => e.EventType == AIUsageEventType.OperationStarted),
                    CompletedOperations = relevantEvents.Count(e => e.EventType == AIUsageEventType.OperationCompleted),
                    FailedOperations = relevantEvents.Count(e => e.EventType == AIUsageEventType.OperationFailed),
                    AverageOperationDuration = CalculateAverageOperationDuration(relevantEvents),
                    MostUsedOperationType = GetMostUsedOperationType(relevantEvents),
                    MostUsedPlatform = GetMostUsedPlatform(relevantEvents),
                    SuccessRate = CalculateSuccessRate(relevantEvents),
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Generated AI usage statistics: {TotalOperations} operations, {SuccessRate}% success rate", 
                    statistics.TotalOperations, statistics.SuccessRate);

                await Task.Delay(10);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI usage statistics");
                throw;
            }
        }

        /// <summary>
        /// Generates usage analytics and insights
        /// </summary>
        public async Task<AIUsageAnalytics> GenerateAnalyticsAsync(TimeSpan? timeRange = null, string? userId = null)
        {
            try
            {
                _logger.LogDebug("Generating AI usage analytics");

                var statistics = await GetUsageStatisticsAsync(timeRange, userId);
                var activeOperations = await GetActiveOperationsAsync();

                var analytics = new AIUsageAnalytics
                {
                    Statistics = statistics,
                    ActiveOperations = activeOperations.Count,
                    PeakUsageTime = CalculatePeakUsageTime(statistics),
                    UsageTrends = CalculateUsageTrends(statistics),
                    PerformanceInsights = GeneratePerformanceInsights(statistics),
                    Recommendations = GenerateUsageRecommendations(statistics),
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Generated AI usage analytics with {Insights} insights and {Recommendations} recommendations", 
                    analytics.PerformanceInsights.Count, analytics.Recommendations.Count);

                await Task.Delay(10);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI usage analytics");
                throw;
            }
        }

        private TimeSpan CalculateAverageOperationDuration(List<AIUsageEvent> events)
        {
            var completedEvents = events.Where(e => e.EventType == AIUsageEventType.OperationCompleted).ToList();
            if (!completedEvents.Any())
                return TimeSpan.Zero;

            var totalDuration = completedEvents
                .Where(e => e.Details.ContainsKey("Duration"))
                .Sum(e => Convert.ToDouble(e.Details["Duration"]));

            return TimeSpan.FromMilliseconds(totalDuration / completedEvents.Count);
        }

        private string GetMostUsedOperationType(List<AIUsageEvent> events)
        {
            // In a real implementation, this would analyze the actual data
            return "CodeGeneration";
        }

        private string GetMostUsedPlatform(List<AIUsageEvent> events)
        {
            // In a real implementation, this would analyze the actual data
            return "WebAssembly";
        }

        private double CalculateSuccessRate(List<AIUsageEvent> events)
        {
            var totalOperations = events.Count(e => e.EventType == AIUsageEventType.OperationStarted);
            var completedOperations = events.Count(e => e.EventType == AIUsageEventType.OperationCompleted);
            
            if (totalOperations == 0)
                return 0;

            return (double)completedOperations / totalOperations * 100;
        }

        private DateTime CalculatePeakUsageTime(AIUsageStatistics statistics)
        {
            // In a real implementation, this would calculate actual peak usage time
            return DateTime.UtcNow.AddHours(-2);
        }

        private List<string> CalculateUsageTrends(AIUsageStatistics statistics)
        {
            var trends = new List<string>();

            if (statistics.SuccessRate > 95)
                trends.Add("High success rate indicates stable AI operations");
            
            if (statistics.TotalOperations > 1000)
                trends.Add("High usage volume suggests good adoption");
            
            if (statistics.AverageOperationDuration.TotalSeconds < 5)
                trends.Add("Fast operation times indicate good performance");

            return trends;
        }

        private List<string> GeneratePerformanceInsights(AIUsageStatistics statistics)
        {
            var insights = new List<string>();

            if (statistics.SuccessRate < 90)
                insights.Add("Consider investigating failed operations to improve success rate");

            if (statistics.AverageOperationDuration.TotalSeconds > 30)
                insights.Add("Long operation times may indicate performance issues");

            if (statistics.TotalOperations == 0)
                insights.Add("No AI operations detected - consider promoting AI features");

            return insights;
        }

        private List<string> GenerateUsageRecommendations(AIUsageStatistics statistics)
        {
            var recommendations = new List<string>();

            if (statistics.SuccessRate < 95)
                recommendations.Add("Implement better error handling and retry mechanisms");

            if (statistics.AverageOperationDuration.TotalSeconds > 10)
                recommendations.Add("Consider optimizing AI models or increasing resources");

            if (statistics.TotalOperations > 5000)
                recommendations.Add("Consider implementing rate limiting for high-volume users");

            return recommendations;
        }
    }
}
