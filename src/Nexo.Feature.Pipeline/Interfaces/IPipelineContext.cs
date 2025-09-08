using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Universal state management interface for pipeline execution context.
    /// Provides shared data store, execution tracking, and configuration management.
    /// </summary>
    public interface IPipelineContext
    {
        /// <summary>
        /// Unique execution identifier for this pipeline run.
        /// </summary>
        string ExecutionId { get; }
        
        /// <summary>
        /// Timestamp when the pipeline execution started.
        /// </summary>
        DateTime StartTime { get; }
        
        /// <summary>
        /// Current execution status of the pipeline.
        /// </summary>
        PipelineExecutionStatus Status { get; set; }
        
        /// <summary>
        /// Shared data store for communication between commands and behaviors.
        /// Thread-safe concurrent dictionary for storing arbitrary data.
        /// </summary>
        ConcurrentDictionary<string, object> SharedData { get; }
        
        /// <summary>
        /// Configuration settings for the current pipeline execution.
        /// </summary>
        IPipelineConfiguration Configuration { get; }
        
        /// <summary>
        /// Logger instance for structured logging throughout the pipeline.
        /// </summary>
        ILogger Logger { get; }
        
        /// <summary>
        /// Cancellation token for pipeline execution.
        /// </summary>
        CancellationToken CancellationToken { get; }
        
        /// <summary>
        /// Gets a value from the shared data store with type safety.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="defaultValue">Default value if key doesn't exist.</param>
        /// <returns>The value if found and of correct type, otherwise the default value.</returns>
        T? GetValue<T>(string key, T? defaultValue = default(T));
        
        /// <summary>
        /// Sets a value in the shared data store.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key to store.</param>
        /// <param name="value">The value to store.</param>
        void SetValue<T>(string key, T value);
        
        /// <summary>
        /// Removes a value from the shared data store.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was found and removed, false otherwise.</returns>
        bool RemoveValue(string key);
        
        /// <summary>
        /// Checks if a key exists in the shared data store.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        bool HasValue(string key);
        
        /// <summary>
        /// Gets execution metrics for the current pipeline run.
        /// </summary>
        /// <returns>Pipeline execution metrics.</returns>
        PipelineExecutionMetrics GetMetrics();
        
        /// <summary>
        /// Adds an execution step to the pipeline history.
        /// </summary>
        /// <param name="step">The execution step to add.</param>
        void AddExecutionStep(PipelineExecutionStep step);
        
        /// <summary>
        /// Gets the execution history for this pipeline run.
        /// </summary>
        /// <returns>List of execution steps.</returns>
        IReadOnlyList<PipelineExecutionStep> GetExecutionHistory();
    }
} 