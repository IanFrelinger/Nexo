using System;
using System.Collections.Generic;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a phase of pipeline execution that groups related aggregators.
    /// </summary>
    public class PipelineExecutionPhase
    {
        /// <summary>
        /// Gets or sets the phase number (order of execution).
        /// </summary>
        public int PhaseNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the phase.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the description of what this phase does.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the execution strategy for this phase.
        /// </summary>
        public AggregatorExecutionStrategy ExecutionStrategy { get; set; }
        
        /// <summary>
        /// Gets or sets the list of aggregator IDs in this phase.
        /// </summary>
        public List<string> Aggregators { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets whether aggregators in this phase can be executed in parallel.
        /// </summary>
        public bool CanExecuteInParallel { get; set; }
        
        /// <summary>
        /// Gets or sets the estimated execution time for this phase in milliseconds.
        /// </summary>
        public long EstimatedExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the dependencies that must be satisfied before this phase can execute.
        /// </summary>
        public List<ExecutionDependency> Dependencies { get; set; } = new List<ExecutionDependency>();
        
        /// <summary>
        /// Gets or sets additional metadata about this phase.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets the conditions that must be met for this phase to execute.
        /// </summary>
        public List<string> Conditions { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the resource requirements for this phase.
        /// </summary>
        public ResourceRequirements ResourceRequirements { get; set; } = new ResourceRequirements();
        
        /// <summary>
        /// Gets or sets the timeout for this phase in milliseconds.
        /// </summary>
        public long TimeoutMs { get; set; } = 300000; // 5 minutes default
        
        /// <summary>
        /// Gets or sets the maximum number of retries for this phase.
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the delay between retries in milliseconds.
        /// </summary>
        public long RetryDelayMs { get; set; } = 1000; // 1 second default
        
        /// <summary>
        /// Gets or sets whether this phase is optional.
        /// </summary>
        public bool IsOptional { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the priority of this phase.
        /// </summary>
        public CommandPriority Priority { get; set; } = CommandPriority.Normal;
        
        /// <summary>
        /// Creates a new execution phase.
        /// </summary>
        /// <param name="phaseNumber">The phase number.</param>
        /// <param name="name">The phase name.</param>
        /// <param name="executionStrategy">The execution strategy.</param>
        /// <returns>A new execution phase.</returns>
        public static PipelineExecutionPhase Create(int phaseNumber, string name, AggregatorExecutionStrategy executionStrategy)
        {
            return new PipelineExecutionPhase
            {
                PhaseNumber = phaseNumber,
                Name = name,
                ExecutionStrategy = executionStrategy,
                CanExecuteInParallel = executionStrategy == AggregatorExecutionStrategy.Parallel
            };
        }
        
        /// <summary>
        /// Adds an aggregator to this phase.
        /// </summary>
        /// <param name="aggregatorId">The aggregator ID to add.</param>
        /// <returns>This execution phase for method chaining.</returns>
        public PipelineExecutionPhase AddAggregator(string aggregatorId)
        {
            if (!string.IsNullOrWhiteSpace(aggregatorId) && !Aggregators.Contains(aggregatorId))
            {
                Aggregators.Add(aggregatorId);
            }
            return this;
        }
        
        /// <summary>
        /// Adds a dependency to this phase.
        /// </summary>
        /// <param name="dependency">The dependency to add.</param>
        /// <returns>This execution phase for method chaining.</returns>
        public PipelineExecutionPhase AddDependency(ExecutionDependency dependency)
        {
            if (dependency != null)
            {
                Dependencies.Add(dependency);
            }
            return this;
        }
        
        /// <summary>
        /// Adds metadata to this phase.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>This execution phase for method chaining.</returns>
        public PipelineExecutionPhase AddMetadata(string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                Metadata[key] = value;
            }
            return this;
        }
        
        /// <summary>
        /// Sets the resource requirements for this phase.
        /// </summary>
        /// <param name="requirements">The resource requirements.</param>
        /// <returns>This execution phase for method chaining.</returns>
        public PipelineExecutionPhase SetResourceRequirements(ResourceRequirements requirements)
        {
            ResourceRequirements = requirements ?? new ResourceRequirements();
            return this;
        }
        
        /// <summary>
        /// Sets the timeout for this phase.
        /// </summary>
        /// <param name="timeoutMs">The timeout in milliseconds.</param>
        /// <returns>This execution phase for method chaining.</returns>
        public PipelineExecutionPhase SetTimeout(long timeoutMs)
        {
            TimeoutMs = timeoutMs > 0 ? timeoutMs : 300000;
            return this;
        }
        
        /// <summary>
        /// Sets the retry configuration for this phase.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="retryDelayMs">The delay between retries in milliseconds.</param>
        /// <returns>This execution phase for method chaining.</returns>
        public PipelineExecutionPhase SetRetryConfiguration(int maxRetries, long retryDelayMs)
        {
            MaxRetries = maxRetries >= 0 ? maxRetries : 3;
            RetryDelayMs = retryDelayMs > 0 ? retryDelayMs : 1000;
            return this;
        }
    }
} 