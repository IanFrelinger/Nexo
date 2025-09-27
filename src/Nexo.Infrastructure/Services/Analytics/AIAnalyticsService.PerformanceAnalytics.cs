using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Analytics;

namespace Nexo.Infrastructure.Services.Analytics
{
    /// <summary>
    /// Performance analytics functionality
    /// </summary>
    public partial class AIAnalyticsService
    {
        /// <summary>
        /// Gets performance analytics for a specific time period.
        /// </summary>
        public async Task<AIPerformanceAnalytics> GetPerformanceAnalyticsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            lock (_lock)
            {
                var metrics = _performanceMetrics
                    .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                    .ToList();

                var analytics = new AIPerformanceAnalytics
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    TotalMetrics = metrics.Count,
                    AverageLatency = metrics.Where(m => m.Latency.HasValue).Select(m => m.Latency!.Value).Aggregate(TimeSpan.Zero, (sum, time) => sum + time) / Math.Max(metrics.Count(m => m.Latency.HasValue), 1),
                    AverageThroughput = metrics.Where(m => m.Throughput.HasValue).Average(m => m.Throughput!.Value),
                    AverageAccuracy = metrics.Where(m => m.Accuracy.HasValue).Average(m => m.Accuracy!.Value),
                    ErrorRate = metrics.Count(m => m.IsError) / (double)Math.Max(metrics.Count, 1),
                    ResourceUtilization = CalculateResourceUtilization(metrics),
                    PerformanceTrends = CalculatePerformanceTrends(metrics),
                    Bottlenecks = IdentifyBottlenecks(metrics)
                };

                return analytics;
            }
        }

        /// <summary>
        /// Calculates resource utilization metrics.
        /// </summary>
        private ResourceUtilization CalculateResourceUtilization(List<AIPerformanceMetric> metrics)
        {
            return new ResourceUtilization
            {
                CpuUsage = metrics.Where(m => m.CpuUsage.HasValue).Average(m => m.CpuUsage!.Value),
                MemoryUsage = metrics.Where(m => m.MemoryUsage.HasValue).Average(m => m.MemoryUsage!.Value),
                NetworkUsage = metrics.Where(m => m.NetworkUsage.HasValue).Average(m => m.NetworkUsage!.Value),
                StorageUsage = metrics.Where(m => m.StorageUsage.HasValue).Average(m => m.StorageUsage!.Value)
            };
        }

        /// <summary>
        /// Calculates performance trends over time.
        /// </summary>
        private List<PerformanceTrend> CalculatePerformanceTrends(List<AIPerformanceMetric> metrics)
        {
            var trends = new List<PerformanceTrend>();

            // Group metrics by hour and calculate averages
            var hourlyMetrics = metrics
                .GroupBy(m => m.Timestamp.Date.AddHours(m.Timestamp.Hour))
                .Select(g => new
                {
                    Hour = g.Key,
                    AvgLatency = g.Where(m => m.Latency.HasValue).Select(m => m.Latency!.Value).Aggregate(TimeSpan.Zero, (sum, time) => sum + time) / Math.Max(g.Count(m => m.Latency.HasValue), 1),
                    AvgThroughput = g.Where(m => m.Throughput.HasValue).Average(m => m.Throughput!.Value),
                    ErrorRate = g.Count(m => m.IsError) / (double)Math.Max(g.Count(), 1)
                })
                .OrderBy(x => x.Hour)
                .ToList();

            foreach (var metric in hourlyMetrics)
            {
                trends.Add(new PerformanceTrend
                {
                    Timestamp = metric.Hour,
                    Latency = metric.AvgLatency,
                    Throughput = metric.AvgThroughput,
                    ErrorRate = metric.ErrorRate
                });
            }

            return trends;
        }

        /// <summary>
        /// Identifies performance bottlenecks.
        /// </summary>
        private List<PerformanceBottleneck> IdentifyBottlenecks(List<AIPerformanceMetric> metrics)
        {
            var bottlenecks = new List<PerformanceBottleneck>();

            var avgLatency = metrics.Where(m => m.Latency.HasValue).Select(m => m.Latency!.Value).Aggregate(TimeSpan.Zero, (sum, time) => sum + time) / Math.Max(metrics.Count(m => m.Latency.HasValue), 1);
            var highLatencyMetrics = metrics.Where(m => m.Latency.HasValue && m.Latency.Value > avgLatency * 1.5).ToList();

            if (highLatencyMetrics.Any())
            {
                bottlenecks.Add(new PerformanceBottleneck
                {
                    Type = BottleneckType.HighLatency,
                    Severity = BottleneckSeverity.Medium,
                    Description = $"High latency detected in {highLatencyMetrics.Count} operations",
                    AffectedOperations = highLatencyMetrics.Count,
                    Recommendation = "Consider optimizing model loading or increasing resources"
                });
            }

            var errorRate = metrics.Count(m => m.IsError) / (double)Math.Max(metrics.Count, 1);
            if (errorRate > 0.1)
            {
                bottlenecks.Add(new PerformanceBottleneck
                {
                    Type = BottleneckType.HighErrorRate,
                    Severity = BottleneckSeverity.High,
                    Description = $"High error rate detected: {errorRate:P1}",
                    AffectedOperations = metrics.Count(m => m.IsError),
                    Recommendation = "Investigate error causes and implement better error handling"
                });
            }

            return bottlenecks;
        }
    }
}
