using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for AI-powered domain logic generation from natural language requirements
    /// </summary>
    public interface IDomainLogicGenerator
    {
        /// <summary>
        /// Generates domain logic from validated natural language requirements
        /// </summary>
        /// <param name="requirements">The validated natural language requirements</param>
        /// <param name="domainContext">The domain context for the requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated domain logic with entities, value objects, and business rules</returns>
        Task<DomainLogicResult> GenerateDomainLogicAsync(
            ProcessedRequirements requirements,
            DomainContext domainContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates generated domain logic for consistency and completeness
        /// </summary>
        /// <param name="domainLogic">The generated domain logic</param>
        /// <param name="requirements">The original requirements</param>
        /// <returns>Validation results with summary</returns>
        Task<DomainLogicValidationResult> ValidateDomainLogicAsync(
            DomainLogicResult domainLogic,
            ProcessedRequirements requirements);

        /// <summary>
        /// Optimizes generated domain logic for performance and maintainability
        /// </summary>
        /// <param name="domainLogic">The domain logic to optimize</param>
        /// <param name="optimizationOptions">Optimization options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimized domain logic</returns>
        Task<DomainLogicOptimizationResult> OptimizeDomainLogicAsync(
            DomainLogicResult domainLogic,
            DomainLogicOptimizationOptions optimizationOptions,
            CancellationToken cancellationToken = default);
    }
}