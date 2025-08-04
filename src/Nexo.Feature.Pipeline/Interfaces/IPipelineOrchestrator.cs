using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// High-level orchestrator that coordinates all feature services in the Nexo pipeline.
    /// Provides end-to-end workflow management, error handling, and performance optimization.
    /// </summary>
    public interface IPipelineOrchestrator
    {
        /// <summary>
        /// Executes a complete application development pipeline from requirements to deployment.
        /// </summary>
        /// <param name="request">The pipeline execution request containing all necessary parameters.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Complete pipeline execution result with all artifacts.</returns>
        Task<PipelineExecutionResult> ExecuteApplicationPipelineAsync(
            ApplicationPipelineRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a focused analysis pipeline for existing codebases.
        /// </summary>
        /// <param name="request">The analysis pipeline request.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Analysis pipeline result with insights and recommendations.</returns>
        Task<PipelineExecutionResult> ExecuteAnalysisPipelineAsync(
            AnalysisPipelineRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a performance optimization pipeline.
        /// </summary>
        /// <param name="request">The performance optimization request.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Performance optimization result with metrics and improvements.</returns>
        Task<PipelineExecutionResult> ExecutePerformancePipelineAsync(
            PerformancePipelineRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a platform-specific feature detection and integration pipeline.
        /// </summary>
        /// <param name="request">The platform integration request.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Platform integration result with detected features and capabilities.</returns>
        Task<PipelineExecutionResult> ExecutePlatformPipelineAsync(
            PlatformPipelineRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the pipeline configuration and dependencies.
        /// </summary>
        /// <param name="configuration">The pipeline configuration to validate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validation result with any issues found.</returns>
        Task<Models.PipelineValidationResult> ValidatePipelineConfigurationAsync(
            PipelineConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the health status of all pipeline components.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Health status of all pipeline services.</returns>
        Task<PipelineHealthStatus> GetPipelineHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets comprehensive metrics for the pipeline execution.
        /// </summary>
        /// <param name="executionId">The execution ID to get metrics for.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Pipeline execution metrics.</returns>
        Task<PipelineExecutionMetrics> GetPipelineMetricsAsync(
            string executionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an ongoing pipeline execution.
        /// </summary>
        /// <param name="executionId">The execution ID to cancel.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if cancellation was successful, false otherwise.</returns>
        Task<bool> CancelPipelineAsync(
            string executionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status of a pipeline execution.
        /// </summary>
        /// <param name="executionId">The execution ID to check.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Current pipeline execution status.</returns>
        Task<PipelineExecutionStatus> GetPipelineStatusAsync(
            string executionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a custom pipeline step for extensibility.
        /// </summary>
        /// <param name="step">The custom pipeline step to register.</param>
        void RegisterCustomStep(ICustomPipelineStep step);

        /// <summary>
        /// Gets all available pipeline templates.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>List of available pipeline templates.</returns>
        Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default);
    }
} 