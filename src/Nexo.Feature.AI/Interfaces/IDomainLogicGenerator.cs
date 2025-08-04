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
        /// Extracts business rules from natural language requirements
        /// </summary>
        /// <param name="requirements">The natural language requirements</param>
        /// <param name="domainContext">The domain context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Extracted business rules with categories and confidence scores</returns>
        Task<BusinessRuleExtractionResult> ExtractBusinessRulesAsync(
            ProcessedRequirements requirements,
            DomainContext domainContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates domain entities from requirements and business rules
        /// </summary>
        /// <param name="requirements">The natural language requirements</param>
        /// <param name="businessRules">The extracted business rules</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated domain entities with relationships</returns>
        Task<DomainEntityResult> GenerateDomainEntitiesAsync(
            ProcessedRequirements requirements,
            BusinessRuleExtractionResult businessRules,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates value objects from requirements and business rules
        /// </summary>
        /// <param name="requirements">The natural language requirements</param>
        /// <param name="businessRules">The extracted business rules</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated value objects with validations</returns>
        Task<ValueObjectResult> GenerateValueObjectsAsync(
            ProcessedRequirements requirements,
            BusinessRuleExtractionResult businessRules,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates generated domain logic for consistency and completeness
        /// </summary>
        /// <param name="domainLogic">The generated domain logic</param>
        /// <param name="requirements">The original requirements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation results with summary</returns>
        Task<DomainLogicValidationResult> ValidateDomainLogicAsync(
            DomainLogicResult domainLogic,
            ProcessedRequirements requirements,
            CancellationToken cancellationToken = default);

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