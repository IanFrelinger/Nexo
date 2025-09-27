using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Core test orchestrator interface
    /// </summary>
    public partial interface ITestOrchestrator
    {
        /// <summary>
        /// Executes tests with intelligent orchestration including parallel execution and dependency management.
        /// </summary>
        /// <param name="options">Test orchestration options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Test orchestration result.</returns>
        Task<TestOrchestrationResult> ExecuteTestsAsync(TestOrchestrationOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes tests in parallel with resource optimization.
        /// </summary>
        /// <param name="testFiles">List of test files to execute.</param>
        /// <param name="options">Parallel execution options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Parallel execution result.</returns>
        Task<ParallelExecutionResult> ExecuteTestsInParallelAsync(List<string> testFiles, ParallelExecutionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Orders tests based on dependencies and execution requirements.
        /// </summary>
        /// <param name="testFiles">List of test files to order.</param>
        /// <param name="options">Dependency ordering options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ordered test execution plan.</returns>
        Task<TestExecutionPlan> CreateDependencyOrderedPlanAsync(List<string> testFiles, DependencyOrderingOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs incremental testing based on previous test results and changes.
        /// </summary>
        /// <param name="options">Incremental testing options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Incremental testing result.</returns>
        Task<IncrementalTestingResult> ExecuteIncrementalTestsAsync(IncrementalTestingOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current resource utilization and optimization recommendations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resource utilization information.</returns>
        Task<ResourceUtilization> GetResourceUtilizationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates test orchestration options.
        /// </summary>
        /// <param name="options">Options to validate.</param>
        /// <returns>Validation result.</returns>
        TestOrchestrationValidation ValidateOptions(TestOrchestrationOptions options);
    }
}
