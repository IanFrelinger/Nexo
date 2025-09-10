using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Default implementation of IPipelineContext for universal state management.
    /// </summary>
    public class PipelineContext : IPipelineContext, IIterationPipelineContext
    {
        public string ExecutionId { get; }
        public DateTime StartTime { get; }
        public PipelineExecutionStatus Status { get; set; }
        public ConcurrentDictionary<string, object> SharedData { get; } = new ConcurrentDictionary<string, object>();
        public IPipelineConfiguration Configuration { get; }
        public ILogger Logger { get; }
        public CancellationToken CancellationToken { get; }
        
        // IIterationPipelineContext properties
        public int DataSize { get; set; } = 1000;
        public bool RequiresParallelization { get; set; } = false;
        public PlatformTarget PlatformTarget { get; set; } = PlatformTarget.DotNet;
        public int Priority { get; set; } = 0;
        
        // IIterationPipelineContext SharedData (wraps the ConcurrentDictionary)
        Dictionary<string, object> IIterationPipelineContext.SharedData => 
            new Dictionary<string, object>(SharedData);

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

        public T? GetValue<T>(string key, T? defaultValue = default(T))
        {
            if (SharedData.TryGetValue(key, out var value) && value is T tValue)
                return tValue;
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            SharedData[key] = value!;
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

        /// <summary>
        /// Gets a property value from the shared data store (alias for GetValue)
        /// </summary>
        public T? GetProperty<T>(string key, T? defaultValue = default(T))
        {
            return GetValue(key, defaultValue);
        }

        /// <summary>
        /// Sets a property value in the shared data store (alias for SetValue)
        /// </summary>
        public void SetProperty<T>(string key, T value)
        {
            SetValue(key, value);
        }

        /// <summary>
        /// Static property to get the current pipeline context
        /// </summary>
        public static IPipelineContext? Current { get; set; }
    }

    /// <summary>
    /// Execution context for pipeline processing.
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the runtime environment profile.
        /// </summary>
        public RuntimeEnvironmentProfile Environment { get; set; } = new RuntimeEnvironmentProfile();
    }
} 