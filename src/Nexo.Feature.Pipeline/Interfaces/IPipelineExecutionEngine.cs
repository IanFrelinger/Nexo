using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for pipeline execution engine that orchestrates the execution of aggregators and behaviors
    /// with support for parallel processing, dependency resolution, and performance monitoring.
    /// </summary>
    public interface IPipelineExecutionEngine
    {
        /// <summary>
        /// Executes a pipeline with the specified aggregators.
        /// </summary>
        /// <param name="context">The pipeline execution context.</param>
        /// <param name="aggregatorIds">List of aggregator IDs to execute.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Pipeline execution result.</returns>
        Task<PipelineExecutionResult> ExecuteAsync(
            IPipelineContext context,
            List<string> aggregatorIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates an execution plan for the specified aggregators.
        /// </summary>
        /// <param name="context">The pipeline execution context.</param>
        /// <param name="aggregatorIds">List of aggregator IDs to plan for.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Pipeline execution plan.</returns>
        Task<PipelineExecutionPlan> GenerateExecutionPlanAsync(
            IPipelineContext context,
            List<string> aggregatorIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates dependencies for a given execution plan.
        /// </summary>
        /// <param name="plan">The execution plan to validate.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validation result.</returns>
        Task<ExecutionValidationResult> ValidateDependenciesAsync(
            PipelineExecutionPlan plan,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers an aggregator with the execution engine.
        /// </summary>
        /// <param name="aggregator">The aggregator to register.</param>
        void RegisterAggregator(IAggregator aggregator);

        /// <summary>
        /// Registers a behavior with the execution engine.
        /// </summary>
        /// <param name="behavior">The behavior to register.</param>
        void RegisterBehavior(IBehavior behavior);

        /// <summary>
        /// Registers a command with the execution engine.
        /// </summary>
        /// <param name="command">The command to register.</param>
        void RegisterCommand(ICommand command);

        /// <summary>
        /// Gets execution metrics for performance monitoring.
        /// </summary>
        /// <returns>List of execution metrics.</returns>
        List<ExecutionMetric> GetExecutionMetrics();

        /// <summary>
        /// Clears all execution metrics.
        /// </summary>
        void ClearMetrics();
    }
} 