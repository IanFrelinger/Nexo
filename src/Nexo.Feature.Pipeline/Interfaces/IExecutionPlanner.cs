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
    /// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
    /// </summary>
    public partial interface IExecutionPlanner
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
}