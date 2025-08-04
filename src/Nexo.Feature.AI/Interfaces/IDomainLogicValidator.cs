using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for comprehensive domain logic validation framework
    /// </summary>
    public interface IDomainLogicValidator
    {
        /// <summary>
        /// Validates generated domain logic for consistency and completeness
        /// </summary>
        /// <param name="domainLogic">The generated domain logic to validate</param>
        /// <param name="requirements">The original requirements for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive validation result</returns>
        Task<DomainLogicValidationResult> ValidateDomainLogicAsync(
            DomainLogicResult domainLogic,
            ProcessedRequirements requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates business rules for consistency and completeness
        /// </summary>
        /// <param name="businessRules">The business rules to validate</param>
        /// <param name="requirements">The original requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Business rule validation result</returns>
        Task<BusinessRuleValidationResult> ValidateBusinessRulesAsync(
            List<BusinessRule> businessRules,
            ProcessedRequirements requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates domain entities for consistency and completeness
        /// </summary>
        /// <param name="entities">The domain entities to validate</param>
        /// <param name="requirements">The original requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Entity validation result</returns>
        Task<EntityValidationResult> ValidateEntitiesAsync(
            List<DomainEntity> entities,
            ProcessedRequirements requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates value objects for consistency and completeness
        /// </summary>
        /// <param name="valueObjects">The value objects to validate</param>
        /// <param name="requirements">The original requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Value object validation result</returns>
        Task<ValueObjectValidationResult> ValidateValueObjectsAsync(
            List<ValueObject> valueObjects,
            ProcessedRequirements requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates consistency across all domain components
        /// </summary>
        /// <param name="domainLogic">The complete domain logic</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Consistency validation result</returns>
        Task<ConsistencyValidationResult> ValidateConsistencyAsync(
            DomainLogic domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates domain logic completeness against requirements
        /// </summary>
        /// <param name="domainLogic">The generated domain logic</param>
        /// <param name="requirements">The original requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completeness validation result</returns>
        Task<CompletenessValidationResult> ValidateCompletenessAsync(
            DomainLogic domainLogic,
            ProcessedRequirements requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes domain logic based on validation results
        /// </summary>
        /// <param name="domainLogic">The domain logic to optimize</param>
        /// <param name="validationResult">The validation results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization suggestions</returns>
        Task<DomainLogicOptimizationResult> OptimizeBasedOnValidationAsync(
            DomainLogicResult domainLogic,
            DomainLogicValidationResult validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates domain logic performance characteristics
        /// </summary>
        /// <param name="domainLogic">The domain logic to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance validation result</returns>
        Task<PerformanceValidationResult> ValidatePerformanceAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates domain logic security characteristics
        /// </summary>
        /// <param name="domainLogic">The domain logic to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Security validation result</returns>
        Task<SecurityValidationResult> ValidateSecurityAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates domain logic architectural patterns
        /// </summary>
        /// <param name="domainLogic">The domain logic to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Architectural validation result</returns>
        Task<ArchitecturalValidationResult> ValidateArchitectureAsync(
            DomainLogicResult domainLogic,
            CancellationToken cancellationToken = default);

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