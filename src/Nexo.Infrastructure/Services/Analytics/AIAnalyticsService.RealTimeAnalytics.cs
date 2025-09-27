using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Analytics;

namespace Nexo.Infrastructure.Services.Analytics
{
    /// <summary>
    /// Real-time analytics functionality
    /// </summary>
    public partial class AIAnalyticsService
    {
        /// <summary>
        /// Gets real-time analytics for the current session.
        /// </summary>
        public async Task<RealTimeAnalytics> GetRealTimeAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            var now = DateTimeOffset.UtcNow;
            var lastHour = now.AddHours(-1);

            lock (_lock)
            {
                var recentEvents = _usageEvents.Where(e => e.Timestamp >= lastHour).ToList();
                var recentMetrics = _performanceMetrics.Where(m => m.Timestamp >= lastHour).ToList();

                return new RealTimeAnalytics
                {
                    Timestamp = now,
                    EventsLastHour = recentEvents.Count,
                    ActiveUsers = recentEvents.Select(e => e.UserId).Distinct().Count(),
                    CurrentThroughput = recentEvents.Count / 60.0, // Events per minute
                    AverageLatency = recentMetrics.Where(m => m.Latency.HasValue).Select(m => m.Latency!.Value).Aggregate(TimeSpan.Zero, (sum, time) => sum + time) / Math.Max(recentMetrics.Count(m => m.Latency.HasValue), 1),
                    ErrorRate = recentMetrics.Count(m => m.IsError) / (double)Math.Max(recentMetrics.Count, 1),
                    SystemHealth = CalculateSystemHealth(recentEvents, recentMetrics)
                };
            }
        }

        /// <summary>
        /// Calculates system health score.
        /// </summary>
        private SystemHealth CalculateSystemHealth(List<AIUsageEvent> events, List<AIPerformanceMetric> metrics)
        {
            var score = 100;

            // Deduct points for errors
            var errorRate = metrics.Count(m => m.IsError) / (double)Math.Max(metrics.Count, 1);
            score -= (int)(errorRate * 50);

            // Deduct points for high latency
            var avgLatency = metrics.Where(m => m.Latency.HasValue).Select(m => m.Latency!.Value).Aggregate(TimeSpan.Zero, (sum, time) => sum + time) / Math.Max(metrics.Count(m => m.Latency.HasValue), 1);
            if (avgLatency > TimeSpan.FromSeconds(5))
                score -= 20;

            // Deduct points for low success rate
            var successRate = events.Count(e => e.Success) / (double)Math.Max(events.Count, 1);
            if (successRate < 0.95)
                score -= 30;

            return new SystemHealth
            {
                Score = Math.Max(score, 0),
                Status = score switch
                {
                    >= 90 => HealthStatus.Excellent,
                    >= 70 => HealthStatus.Good,
                    >= 50 => HealthStatus.Fair,
                    _ => HealthStatus.Poor
                },
                LastUpdated = DateTimeOffset.UtcNow
            };
        }
    }
}
