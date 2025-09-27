using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Analytics;

namespace Nexo.Infrastructure.Services.Analytics
{
    /// <summary>
    /// Usage analytics functionality
    /// </summary>
    public partial class AIAnalyticsService
    {
        /// <summary>
        /// Gets usage analytics for a specific time period.
        /// </summary>
        public async Task<AIUsageAnalytics> GetUsageAnalyticsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            lock (_lock)
            {
                var events = _usageEvents
                    .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                    .ToList();

                var analytics = new AIUsageAnalytics
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    TotalEvents = events.Count,
                    UniqueUsers = events.Select(e => e.UserId).Distinct().Count(),
                    TotalTokens = events.Sum(e => e.TokensUsed),
                    TotalCost = events.Sum(e => e.Cost),
                    AverageResponseTime = events.Where(e => e.ResponseTime.HasValue).Select(e => e.ResponseTime!.Value).Aggregate(TimeSpan.Zero, (sum, time) => sum + time) / Math.Max(events.Count(e => e.ResponseTime.HasValue), 1),
                    SuccessRate = events.Count(e => e.Success) / (double)Math.Max(events.Count, 1),
                    EventsByType = events.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count()),
                    EventsByModel = events.GroupBy(e => e.ModelName).ToDictionary(g => g.Key, g => g.Count()),
                    TopUsers = events.GroupBy(e => e.UserId).OrderByDescending(g => g.Count()).Take(10).Select(g => g.Key).ToList(),
                    HourlyDistribution = GetHourlyDistribution(events),
                    DailyDistribution = GetDailyDistribution(events)
                };

                return analytics;
            }
        }

        /// <summary>
        /// Calculates hourly distribution of events.
        /// </summary>
        private Dictionary<int, int> GetHourlyDistribution(List<AIUsageEvent> events)
        {
            return events
                .GroupBy(e => e.Timestamp.Hour)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Calculates daily distribution of events.
        /// </summary>
        private Dictionary<DateTime, int> GetDailyDistribution(List<AIUsageEvent> events)
        {
            return events
                .GroupBy(e => e.Timestamp.Date)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
