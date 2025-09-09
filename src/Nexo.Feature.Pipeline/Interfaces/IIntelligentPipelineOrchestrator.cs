using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;
using ExecutionContext = Nexo.Feature.Pipeline.Models.ExecutionContext;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Intelligent pipeline orchestrator that provides advanced optimization and monitoring capabilities.
    /// </summary>
    public interface IIntelligentPipelineOrchestrator
    {
        /// <summary>
        /// Executes a pipeline configuration with intelligent optimization.
        /// </summary>
        /// <param name="configuration">The pipeline configuration to execute.</param>
        /// <param name="context">The execution context containing runtime information.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The result of the optimized pipeline execution.</returns>
        Task<PipelineExecutionResult> ExecuteOptimizedAsync(
            PipelineConfiguration configuration,
            ExecutionContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes the performance of a pipeline execution and provides optimization recommendations.
        /// </summary>
        /// <param name="result">The pipeline execution result to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Optimization recommendations based on the execution analysis.</returns>
        Task<OptimizationRecommendation> AnalyzePerformanceAsync(
            PipelineExecutionResult result,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes a pipeline configuration based on historical performance data and current context.
        /// </summary>
        /// <param name="configuration">The configuration to optimize.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An optimized version of the pipeline configuration.</returns>
        Task<PipelineConfiguration> OptimizeConfigurationAsync(
            PipelineConfiguration configuration,
            ExecutionContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an intelligent execution plan for the given configuration.
        /// </summary>
        /// <param name="configuration">The pipeline configuration.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An optimized execution plan with resource allocation and scheduling.</returns>
        Task<ExecutionPlan> CreateExecutionPlanAsync(
            PipelineConfiguration configuration,
            ExecutionContext context,
            CancellationToken cancellationToken = default);
    }
}