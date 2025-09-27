using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services.Execution.Monitoring
{
    /// <summary>
    /// Handles execution metrics collection and management
    /// </summary>
    public partial class MetricsCollector
    {
        private readonly ILogger<PipelineExecutionEngine> _logger;
        private readonly List<ExecutionMetric> _executionMetrics = new List<ExecutionMetric>();
        private readonly object _metricsLock = new object();

        public MetricsCollector(ILogger<PipelineExecutionEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<ExecutionMetric> GetExecutionMetrics()
        {
            lock (_metricsLock)
            {
                return _executionMetrics.ToList();
            }
        }

        public void ClearMetrics()
        {
            lock (_metricsLock)
            {
                _executionMetrics.Clear();
            }
        }

        public void RecordExecutionMetric(string category, string name, double durationMs, int count)
        {
            lock (_metricsLock)
            {
                _executionMetrics.Add(new ExecutionMetric
                {
                    Category = category,
                    Name = name,
                    DurationMs = durationMs,
                    Count = count,
                    Timestamp = DateTime.UtcNow
                });

                // Keep only the last 1000 metrics
                if (_executionMetrics.Count > 1000)
                {
                    _executionMetrics.RemoveRange(0, _executionMetrics.Count - 1000);
                }
            }
        }
    }
}
