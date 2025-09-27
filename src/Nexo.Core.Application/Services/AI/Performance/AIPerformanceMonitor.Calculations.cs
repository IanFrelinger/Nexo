using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Application.Services.AI.Performance
{
    /// <summary>
    /// Performance calculation and analysis functionality for AIPerformanceMonitor.
    /// </summary>
    public partial class AIPerformanceMonitor
    {
        /// <summary>
        /// Gets current CPU usage percentage.
        /// </summary>
        private double GetCurrentCpuUsage()
        {
            try
            {
                // In a real implementation, this would get actual CPU usage
                // For now, return a mock value
                return 25.0; // 25% CPU usage
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Calculates performance score for AI operation metrics.
        /// </summary>
        private double CalculatePerformanceScore(PerformanceMetrics metrics)
        {
            try
            {
                var score = 100.0;

                // Deduct points for long duration
                if (metrics.Duration.TotalSeconds > 10)
                {
                    score -= Math.Min(30, (metrics.Duration.TotalSeconds - 10) * 2);
                }

                // Deduct points for high memory usage
                if (metrics.MemoryDelta > 100 * 1024 * 1024) // 100MB
                {
                    score -= Math.Min(20, (metrics.MemoryDelta - 100 * 1024 * 1024) / (10 * 1024 * 1024));
                }

                // Deduct points for high CPU usage
                if (metrics.CpuDelta > 50)
                {
                    score -= Math.Min(15, (metrics.CpuDelta - 50) / 5);
                }

                // Deduct points for failures
                if (metrics.Status == AIOperationStatus.Failed)
                {
                    score -= 50;
                }

                return Math.Max(0, Math.Min(100, score));
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Calculates performance trend based on historical metrics.
        /// </summary>
        private string CalculatePerformanceTrend(List<PerformanceMetrics> metrics)
        {
            try
            {
                if (metrics.Count < 10)
                    return "Insufficient Data";

                // Get recent metrics (last 20% of data)
                var recentCount = Math.Max(5, metrics.Count / 5);
                var recentMetrics = metrics.TakeLast(recentCount).ToList();
                var olderMetrics = metrics.Take(metrics.Count - recentCount).ToList();

                if (olderMetrics.Count == 0)
                    return "Insufficient Data";

                var recentAverage = recentMetrics.Average(m => m.PerformanceScore);
                var olderAverage = olderMetrics.Average(m => m.PerformanceScore);

                var difference = recentAverage - olderAverage;

                if (difference > 5)
                    return "Improving";
                else if (difference < -5)
                    return "Declining";
                else
                    return "Stable";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
