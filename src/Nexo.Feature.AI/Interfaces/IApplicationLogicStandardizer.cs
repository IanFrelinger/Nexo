using Nexo.Feature.AI.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for standardizing application logic into framework-agnostic patterns.
    /// Part of Phase 5.3: Application Logic Standardization.
    /// </summary>
    public interface IApplicationLogicStandardizer
    {
        /// <summary>
        /// Standardizes domain logic into framework-agnostic application patterns.
        /// </summary>
        /// <param name="domainLogic">The domain logic to standardize</param>
        /// <param name="standardizationOptions">Options for the standardization process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Standardized application logic result</returns>
        Task<ApplicationLogicStandardizationResult> StandardizeApplicationLogicAsync(
            DomainLogic domainLogic,
            ApplicationLogicStandardizationOptions standardizationOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies security patterns to the application logic.
        /// </summary>
        /// <param name="securityOptions">Security configuration options</param>
        /// <returns>Secured application logic result</returns>
        Task<SecurityPatternResult> ApplySecurityPatternsAsync(SecurityPatternOptions securityOptions);

        /// <summary>
        /// Optimizes application logic for performance.
        /// </summary>
        /// <param name="performanceOptions">Performance optimization options</param>
        /// <returns>Performance optimized application logic result</returns>
        Task<PerformanceOptimizationResult> OptimizeForPerformanceAsync(PerformanceOptimizationOptions performanceOptions);

        /// <summary>
        /// Generates state management architecture for the application logic.
        /// </summary>
        /// <param name="stateManagementOptions">State management configuration options</param>
        /// <returns>State management architecture result</returns>
        Task<StateManagementResult> GenerateStateManagementAsync(StateManagementOptions stateManagementOptions);

        /// <summary>
        /// Creates API contracts for the application logic.
        /// </summary>
        /// <param name="apiContractOptions">API contract configuration options</param>
        /// <returns>API contract generation result</returns>
        Task<ApiContractResult> GenerateApiContractsAsync(ApiContractOptions apiContractOptions);

        /// <summary>
        /// Optimizes data flow in the application logic.
        /// </summary>
        /// <param name="dataFlowOptions">Data flow optimization options</param>
        /// <returns>Data flow optimization result</returns>
        Task<DataFlowOptimizationResult> OptimizeDataFlowAsync(DataFlowOptimizationOptions dataFlowOptions);

        /// <summary>
        /// Integrates caching strategies into the application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to add caching to</param>
        /// <param name="cachingOptions">Caching configuration options</param>
        /// <returns>Caching strategy integration result</returns>
        Task<CachingStrategyResult> IntegrateCachingStrategiesAsync(
            StandardizedApplicationLogic applicationLogic,
            CachingStrategyOptions cachingOptions);

        /// <summary>
        /// Validates the standardized application logic.
        /// </summary>
        /// <param name="applicationLogic">The application logic to validate</param>
        /// <param name="validationOptions">Validation configuration options</param>
        /// <returns>Application logic validation result</returns>
        Task<ApplicationLogicValidationResult> ValidateApplicationLogicAsync(
            StandardizedApplicationLogic applicationLogic,
            ApplicationLogicValidationOptions validationOptions);

        /// <summary>
        /// Gets supported application patterns.
        /// </summary>
        /// <returns>List of supported application patterns</returns>
        IEnumerable<ApplicationPattern> GetSupportedPatterns();

        /// <summary>
        /// Gets supported security patterns.
        /// </summary>
        /// <returns>List of supported security patterns</returns>
        IEnumerable<SecurityPattern> GetSupportedSecurityPatterns();

        /// <summary>
        /// Gets supported state management patterns.
        /// </summary>
        /// <returns>List of supported state management patterns</returns>
        IEnumerable<StateManagementPattern> GetSupportedStateManagementPatterns();
    }
} 