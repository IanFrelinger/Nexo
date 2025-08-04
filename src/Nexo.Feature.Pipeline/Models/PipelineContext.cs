using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Default implementation of IPipelineContext for universal state management.
    /// </summary>
    public class PipelineContext : IPipelineContext
    {
        public string ExecutionId { get; }
        public DateTime StartTime { get; }
        public PipelineExecutionStatus Status { get; set; }
        public ConcurrentDictionary<string, object> SharedData { get; } = new ConcurrentDictionary<string, object>();
        public IPipelineConfiguration Configuration { get; }
        public ILogger Logger { get; }
        public CancellationToken CancellationToken { get; }

        private readonly List<PipelineExecutionStep> _executionHistory = new List<PipelineExecutionStep>();
        private readonly object _historyLock = new object();
        private readonly PipelineExecutionMetrics _metrics = new PipelineExecutionMetrics();

        public PipelineContext(
            ILogger logger,
            IPipelineConfiguration configuration,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            ExecutionId = Guid.NewGuid().ToString();
            StartTime = DateTime.UtcNow;
            Status = PipelineExecutionStatus.NotStarted;
            CancellationToken = cancellationToken;
            _metrics.StartTime = StartTime;
        }

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (SharedData.TryGetValue(key, out var value) && value is T tValue)
                return tValue;
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            SharedData[key] = value;
        }

        public bool RemoveValue(string key)
        {
            return SharedData.TryRemove(key, out _);
        }

        public bool HasValue(string key)
        {
            return SharedData.ContainsKey(key);
        }

        public PipelineExecutionMetrics GetMetrics()
        {
            lock (_historyLock)
            {
                _metrics.EndTime = DateTime.UtcNow;
                return _metrics;
            }
        }

        public void AddExecutionStep(PipelineExecutionStep step)
        {
            lock (_historyLock)
            {
                _executionHistory.Add(step);
                // Optionally update metrics here
            }
        }

        public IReadOnlyList<PipelineExecutionStep> GetExecutionHistory()
        {
            lock (_historyLock)
            {
                return _executionHistory.ToList();
            }
        }
    }
} 