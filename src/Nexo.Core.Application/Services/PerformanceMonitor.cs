using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// Performance monitoring service for tracking application metrics.
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private readonly Timer _cleanupTimer;
        private readonly object _lock = new object();

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
            
            // Cleanup old metrics every 5 minutes
            _cleanupTimer = new Timer(CleanupOldMetrics, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Starts monitoring a performance metric.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="category">The category of the metric.</param>
        /// <returns>A disposable object that stops monitoring when disposed.</returns>
        public IDisposable StartMonitoring(string metricName, string category = "default")
        {
            var key = $"{category}:{metricName}";
            var stopwatch = Stopwatch.StartNew();
            
            return new MetricTracker(key, stopwatch, this);
        }

        /// <summary>
        /// Records a performance metric.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="durationMs">The duration in milliseconds.</param>
        /// <param name="category">The category of the metric.</param>
        /// <param name="metadata">Additional metadata.</param>
        public void RecordMetric(string metricName, long durationMs, string category = "default", object metadata = null)
        {
            var key = $"{category}:{metricName}";
            
            _metrics.AddOrUpdate(key, 
                new PerformanceMetric(metricName, category, durationMs, metadata),
                (k, existing) => existing.AddMeasurement(durationMs, metadata));
            
            _logger.LogDebug("Recorded metric {MetricName} in category {Category}: {DurationMs}ms", 
                metricName, category, durationMs);
        }

        /// <summary>
        /// Gets performance metrics for a category.
        /// </summary>
        /// <param name="category">The category to get metrics for.</param>
        /// <returns>An array of performance metrics.</returns>
        public PerformanceMetric[] GetMetrics(string category = "default")
        {
            return _metrics.Values
                .Where(m => m.Category == category)
                .ToArray();
        }

        /// <summary>
        /// Gets all performance metrics.
        /// </summary>
        /// <returns>An array of all performance metrics.</returns>
        public PerformanceMetric[] GetAllMetrics()
        {
            return _metrics.Values.ToArray();
        }

        /// <summary>
        /// Clears all performance metrics.
        /// </summary>
        public void ClearMetrics()
        {
            _metrics.Clear();
            _logger.LogInformation("Cleared all performance metrics");
        }

        /// <summary>
        /// Gets a summary of performance metrics.
        /// </summary>
        /// <returns>A performance summary.</returns>
        public PerformanceSummary GetSummary()
        {
            var metrics = GetAllMetrics();
            var summary = new PerformanceSummary
            {
                TotalMetrics = metrics.Length,
                Categories = metrics.GroupBy(m => m.Category).Count(),
                AverageResponseTimeMs = metrics.Any() ? metrics.Average(m => m.AverageDurationMs) : 0,
                TotalRequests = metrics.Sum(m => m.RequestCount),
                SlowestMetric = metrics.OrderByDescending(m => m.MaxDurationMs).FirstOrDefault(),
                FastestMetric = metrics.OrderBy(m => m.MinDurationMs).FirstOrDefault()
            };

            return summary;
        }

        private void CleanupOldMetrics(object state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-1); // Keep metrics for 1 hour
                var keysToRemove = _metrics
                    .Where(kvp => kvp.Value.LastUpdated < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToArray();

                foreach (var key in keysToRemove)
                {
                    _metrics.TryRemove(key, out _);
                }

                if (keysToRemove.Length > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} old performance metrics", keysToRemove.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during performance metrics cleanup");
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        /// <summary>
        /// Internal class for tracking individual metrics.
        /// </summary>
        private class MetricTracker : IDisposable
        {
            private readonly string _key;
            private readonly Stopwatch _stopwatch;
            private readonly PerformanceMonitor _monitor;

            public MetricTracker(string key, Stopwatch stopwatch, PerformanceMonitor monitor)
            {
                _key = key;
                _stopwatch = stopwatch;
                _monitor = monitor;
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _monitor.RecordMetric(_key, _stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Represents a performance metric.
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; }
        public string Category { get; }
        public long MinDurationMs { get; private set; }
        public long MaxDurationMs { get; private set; }
        public double AverageDurationMs { get; private set; }
        public long TotalDurationMs { get; private set; }
        public int RequestCount { get; private set; }
        public DateTime LastUpdated { get; private set; }
        public object Metadata { get; private set; }

        public PerformanceMetric(string name, string category, long durationMs, object metadata = null)
        {
            Name = name;
            Category = category;
            MinDurationMs = durationMs;
            MaxDurationMs = durationMs;
            AverageDurationMs = durationMs;
            TotalDurationMs = durationMs;
            RequestCount = 1;
            LastUpdated = DateTime.UtcNow;
            Metadata = metadata;
        }

        public PerformanceMetric AddMeasurement(long durationMs, object metadata = null)
        {
            MinDurationMs = Math.Min(MinDurationMs, durationMs);
            MaxDurationMs = Math.Max(MaxDurationMs, durationMs);
            TotalDurationMs += durationMs;
            RequestCount++;
            AverageDurationMs = (double)TotalDurationMs / RequestCount;
            LastUpdated = DateTime.UtcNow;
            
            if (metadata != null)
            {
                Metadata = metadata;
            }

            return this;
        }
    }

    /// <summary>
    /// Represents a performance summary.
    /// </summary>
    public class PerformanceSummary
    {
        public int TotalMetrics { get; set; }
        public int Categories { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public long TotalRequests { get; set; }
        public PerformanceMetric SlowestMetric { get; set; }
        public PerformanceMetric FastestMetric { get; set; }
    }
} 