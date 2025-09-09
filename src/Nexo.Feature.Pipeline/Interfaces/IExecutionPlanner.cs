using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;
using ExecutionContext = Nexo.Feature.Pipeline.Models.ExecutionContext;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for planning pipeline execution strategies.
    /// </summary>
    public interface IExecutionPlanner
    {
        /// <summary>
        /// Creates an execution plan for the given pipeline configuration.
        /// </summary>
        /// <param name="configuration">The pipeline configuration.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Execution plan for the pipeline.</returns>
        Task<ExecutionPlan> CreateExecutionPlanAsync(
            PipelineConfiguration configuration,
            ExecutionContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes an existing execution plan.
        /// </summary>
        /// <param name="plan">The execution plan to optimize.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Optimized execution plan.</returns>
        Task<ExecutionPlan> OptimizeExecutionPlanAsync(
            ExecutionPlan plan,
            ExecutionContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an execution plan for feasibility.
        /// </summary>
        /// <param name="plan">The execution plan to validate.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validation result for the execution plan.</returns>
        Task<ExecutionPlanValidationResult> ValidateExecutionPlanAsync(
            ExecutionPlan plan,
            ExecutionContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Execution plan for pipeline processing.
    /// </summary>
    public class ExecutionPlan
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
    public class ExecutionStep
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
    /// Execution strategy for pipeline processing.
    /// </summary>
    public enum ExecutionStrategy
    {
        /// <summary>
        /// Sequential execution.
        /// </summary>
        Sequential,

        /// <summary>
        /// Parallel execution.
        /// </summary>
        Parallel,

        /// <summary>
        /// Hybrid execution (mix of sequential and parallel).
        /// </summary>
        Hybrid,

        /// <summary>
        /// Adaptive execution (strategy changes based on conditions).
        /// </summary>
        Adaptive
    }

    /// <summary>
    /// Types of execution steps.
    /// </summary>
    public enum ExecutionStepType
    {
        /// <summary>
        /// Data processing step.
        /// </summary>
        DataProcessing,

        /// <summary>
        /// Validation step.
        /// </summary>
        Validation,

        /// <summary>
        /// Transformation step.
        /// </summary>
        Transformation,

        /// <summary>
        /// Aggregation step.
        /// </summary>
        Aggregation,

        /// <summary>
        /// Filtering step.
        /// </summary>
        Filtering,

        /// <summary>
        /// Sorting step.
        /// </summary>
        Sorting,

        /// <summary>
        /// Custom step.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Step priority levels.
    /// </summary>
    public enum StepPriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Retry policy for execution steps.
    /// </summary>
    public class RetryPolicy
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

    /// <summary>
    /// Retry strategies for execution steps.
    /// </summary>
    public enum RetryStrategy
    {
        /// <summary>
        /// Fixed delay between retries.
        /// </summary>
        FixedDelay,

        /// <summary>
        /// Exponential backoff delay.
        /// </summary>
        ExponentialBackoff,

        /// <summary>
        /// Linear backoff delay.
        /// </summary>
        LinearBackoff,

        /// <summary>
        /// Custom retry strategy.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Execution plan validation result.
    /// </summary>
    public class ExecutionPlanValidationResult
    {
        /// <summary>
        /// Gets or sets the validation identifier.
        /// </summary>
        public string ValidationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the plan identifier.
        /// </summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the plan is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets the validation timestamp.
        /// </summary>
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Validation error for execution plan.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets the error identifier.
        /// </summary>
        public string ErrorId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error severity.
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

        /// <summary>
        /// Gets or sets the affected step or component.
        /// </summary>
        public string AffectedComponent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation warning for execution plan.
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Gets or sets the warning identifier.
        /// </summary>
        public string WarningId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the warning code.
        /// </summary>
        public string WarningCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warning message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warning severity.
        /// </summary>
        public WarningSeverity Severity { get; set; } = WarningSeverity.Warning;

        /// <summary>
        /// Gets or sets the affected step or component.
        /// </summary>
        public string AffectedComponent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Error severity levels.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Warning severity levels.
    /// </summary>
    public enum WarningSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,

        /// <summary>
        /// High warning level.
        /// </summary>
        HighWarning
    }
}
