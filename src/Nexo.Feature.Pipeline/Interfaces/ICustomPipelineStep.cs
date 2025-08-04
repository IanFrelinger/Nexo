using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for custom pipeline steps that can be registered with the orchestrator.
    /// Provides extensibility for custom workflows and integrations.
    /// </summary>
    public interface ICustomPipelineStep
    {
        /// <summary>
        /// Unique identifier for this custom step.
        /// </summary>
        string StepId { get; }
        
        /// <summary>
        /// Human-readable name for this step.
        /// </summary>
        string StepName { get; }
        
        /// <summary>
        /// Description of what this step does.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Category this step belongs to (e.g., "Analysis", "Generation", "Optimization").
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// Priority level for this step execution.
        /// </summary>
        CommandPriority Priority { get; }
        
        /// <summary>
        /// Estimated duration for this step execution.
        /// </summary>
        TimeSpan EstimatedDuration { get; }
        
        /// <summary>
        /// Whether this step can be executed in parallel with other steps.
        /// </summary>
        bool CanExecuteInParallel { get; }
        
        /// <summary>
        /// Dependencies that must be completed before this step can execute.
        /// </summary>
        List<string> Dependencies { get; }
        
        /// <summary>
        /// Validates the step configuration and inputs.
        /// </summary>
        /// <param name="context">The pipeline context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validation result.</returns>
        Task<CustomStepValidationResult> ValidateAsync(
            IPipelineContext context,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes the custom pipeline step.
        /// </summary>
        /// <param name="context">The pipeline context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Execution result.</returns>
        Task<CustomStepExecutionResult> ExecuteAsync(
            IPipelineContext context,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Performs cleanup after step execution.
        /// </summary>
        /// <param name="context">The pipeline context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Cleanup result.</returns>
        Task<CustomStepCleanupResult> CleanupAsync(
            IPipelineContext context,
            CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Result of custom step validation.
    /// </summary>
    public class CustomStepValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public Dictionary<string, object> ValidationData { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Result of custom step execution.
    /// </summary>
    public class CustomStepExecutionResult
    {
        public bool IsSuccess { get; set; }
        public string StepId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        public Dictionary<string, object> Outputs { get; set; } = new Dictionary<string, object>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Result of custom step cleanup.
    /// </summary>
    public class CustomStepCleanupResult
    {
        public bool IsSuccess { get; set; }
        public List<string> CleanedUpResources { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
} 