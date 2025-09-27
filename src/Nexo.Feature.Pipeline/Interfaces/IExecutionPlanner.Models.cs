using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Execution plan for pipeline processing.
    /// </summary>
    public partial class ExecutionPlan
    {
        /// <summary>
        /// Gets or sets the plan identifier.
        /// </summary>
        public string PlanId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the pipeline configuration ID.
        /// </summary>
        public string PipelineConfigurationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution strategy.
        /// </summary>
        public ExecutionStrategy Strategy { get; set; } = ExecutionStrategy.Sequential;

        /// <summary>
        /// Gets or sets the execution steps.
        /// </summary>
        public List<ExecutionStep> Steps { get; set; } = new();

        /// <summary>
        /// Gets or sets the estimated execution time.
        /// </summary>
        public TimeSpan EstimatedExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the resource requirements.
        /// </summary>
        public ResourceRequirements ResourceRequirements { get; set; } = new();

        /// <summary>
        /// Gets or sets the plan creation timestamp.
        /// </summary>
        public DateTime CreationTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the plan optimization level.
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Balanced;
    }

    /// <summary>
    /// Execution step within a pipeline.
    /// </summary>
    public partial class ExecutionStep
    {
        /// <summary>
        /// Gets or sets the step identifier.
        /// </summary>
        public string StepId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the step name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the step description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the step type.
        /// </summary>
        public ExecutionStepType Type { get; set; }

        /// <summary>
        /// Gets or sets the step priority.
        /// </summary>
        public StepPriority Priority { get; set; } = StepPriority.Normal;

        /// <summary>
        /// Gets or sets the estimated execution time.
        /// </summary>
        public TimeSpan EstimatedExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the resource requirements for this step.
        /// </summary>
        public ResourceRequirements ResourceRequirements { get; set; } = new();

        /// <summary>
        /// Gets or sets the dependencies for this step.
        /// </summary>
        public List<string> Dependencies { get; set; } = new();

        /// <summary>
        /// Gets or sets the step parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets whether this step can be executed in parallel.
        /// </summary>
        public bool CanExecuteInParallel { get; set; } = false;

        /// <summary>
        /// Gets or sets the retry policy for this step.
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; } = new();
    }

    /// <summary>
    /// Retry policy for execution steps.
    /// </summary>
    public partial class RetryPolicy
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial retry delay.
        /// </summary>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum retry delay.
        /// </summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the retry delay multiplier.
        /// </summary>
        public double RetryDelayMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the retry strategy.
        /// </summary>
        public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
    }
}
