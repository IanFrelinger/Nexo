using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Adaptation
{
    /// <summary>
    /// In-memory implementation of performance data store
    /// </summary>
    public partial class InMemoryPerformanceDataStore : IPerformanceDataStore
    {
        private readonly List<PerformanceData> _performanceData = new();
        private readonly object _lock = new();
        
        public Task StorePerformanceDataAsync(PerformanceData data)
        {
            lock (_lock)
            {
                _performanceData.Add(data);
            }
            return Task.CompletedTask;
        }
        
        public Task<IEnumerable<PerformanceData>> GetRecentPerformanceDataAsync(TimeSpan timeWindow)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            lock (_lock)
            {
                return Task.FromResult(_performanceData.Where(p => p.Timestamp >= cutoff));
            }
        }
        
        public Task StorePerformanceDataAsync(IEnumerable<PerformanceData> data)
        {
            lock (_lock)
            {
                _performanceData.AddRange(data);
            }
            return Task.CompletedTask;
        }
        
        public Task<PerformanceData?> GetPerformanceDataAsync(string id)
        {
            lock (_lock)
            {
                return Task.FromResult(_performanceData.FirstOrDefault(p => p.Id == id));
            }
        }
        
        public Task<IEnumerable<PerformanceData>> GetPerformanceDataAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                return Task.FromResult(_performanceData.Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime));
            }
        }
        
        public Task<IEnumerable<PerformanceData>> GetPerformanceDataByMetricAsync(string metricName, DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                return Task.FromResult(_performanceData.Where(p => p.MetricName == metricName && p.Timestamp >= startTime && p.Timestamp <= endTime));
            }
        }
        
        public Task<PerformanceData?> GetLatestPerformanceDataAsync(string metricName)
        {
            lock (_lock)
            {
                return Task.FromResult(_performanceData.Where(p => p.MetricName == metricName).OrderByDescending(p => p.Timestamp).FirstOrDefault());
            }
        }
        
        public Task<PerformanceDataSummary> GetPerformanceDataSummaryAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                var data = _performanceData.Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime);
                return Task.FromResult(new PerformanceDataSummary
                {
                    TotalRecords = data.Count(),
                    AverageValue = data.Average(p => p.Value),
                    MinValue = data.Min(p => p.Value),
                    MaxValue = data.Max(p => p.Value),
                    StartTime = startTime,
                    EndTime = endTime
                });
            }
        }
        
        public Task DeleteOldPerformanceDataAsync(DateTime cutoffTime)
        {
            lock (_lock)
            {
                _performanceData.RemoveAll(p => p.Timestamp < cutoffTime);
            }
            return Task.CompletedTask;
        }
        
        public Task<IEnumerable<PerformanceData>> GetHistoricalDataAsync(TimeSpan timeWindow)
        {
            return GetRecentPerformanceDataAsync(timeWindow);
        }
        
        // IPerformanceDataStore interface methods
        public Task<IEnumerable<PerformanceData>> GetRecentPerformanceDataAsync(int count = 10)
        {
            lock (_lock)
            {
                var recent = _performanceData.OrderByDescending(p => p.Timestamp).Take(count);
                return Task.FromResult(recent);
            }
        }
        
        public Task<PerformanceDataSummary> GetPerformanceSummaryAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                var data = _performanceData.Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime);
                return Task.FromResult(new PerformanceDataSummary
                {
                    TotalRecords = data.Count(),
                    AverageValue = data.Any() ? data.Average(p => p.Value) : 0.0,
                    MinValue = data.Any() ? data.Min(p => p.Value) : 0.0,
                    MaxValue = data.Any() ? data.Max(p => p.Value) : 0.0,
                    StartTime = startTime,
                    EndTime = endTime
                });
            }
        }
    }
}
