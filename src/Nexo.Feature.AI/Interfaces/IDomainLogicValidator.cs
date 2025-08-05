using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for a comprehensive domain logic validation framework
    /// </summary>
    public interface IDomainLogicValidator
    {
        /// <summary>
        /// Optimizes domain logic based on validation results
        /// </summary>
        /// <param name="domainLogic">The domain logic to optimize</param>
        /// <param name="validationResult">The validation results</param>
        /// <returns>Optimization suggestions</returns>
        Task<DomainLogicOptimizationResult> OptimizeBasedOnValidationAsync(
            DomainLogicResult domainLogic,
            DomainLogicValidationResult validationResult);

        /// <summary>
        /// Performs comprehensive validation including all validation types
        /// </summary>
        /// <param name="domainLogic">The domain logic to validate</param>
        /// <param name="requirements">The original requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive validation result</returns>
        Task<ComprehensiveValidationResult> ValidateComprehensiveAsync(
            DomainLogicResult domainLogic,
            ProcessedRequirements requirements,
            CancellationToken cancellationToken = default);
    }
}