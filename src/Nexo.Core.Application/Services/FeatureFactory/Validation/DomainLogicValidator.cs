using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.FeatureFactory.Validation.Validators;

namespace Nexo.Core.Application.Services.FeatureFactory.Validation
{
    /// <summary>
    /// Service for validating generated domain logic - orchestrates specialized validators
    /// </summary>
    public partial class DomainLogicValidator : IDomainLogicValidator
    {
        private readonly ILogger<DomainLogicValidator> _logger;
        private readonly EntityValidator _entityValidator;
        private readonly ValueObjectValidator _valueObjectValidator;
        private readonly BusinessRuleValidator _businessRuleValidator;
        private readonly ServiceValidator _serviceValidator;
        private readonly ConsistencyChecker _consistencyChecker;
        private readonly OptimizationAnalyzer _optimizationAnalyzer;
        private readonly ValidationReportGenerator _reportGenerator;

        public DomainLogicValidator(ILogger<DomainLogicValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize specialized validators
            _entityValidator = new EntityValidator(logger);
            _valueObjectValidator = new ValueObjectValidator(logger);
            _businessRuleValidator = new BusinessRuleValidator(logger);
            _serviceValidator = new ServiceValidator(logger);
            _consistencyChecker = new ConsistencyChecker(logger);
            _optimizationAnalyzer = new OptimizationAnalyzer(logger);
            _reportGenerator = new ValidationReportGenerator(logger);
        }

        /// <summary>
        /// Validates complete domain logic by delegating to specialized validators
        /// </summary>
        public async Task<DomainValidationResult> ValidateDomainLogicAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Validating domain logic with {EntityCount} entities, {ValueObjectCount} value objects, {RuleCount} business rules", 
                    domainLogic.Entities.Count, domainLogic.ValueObjects.Count, domainLogic.BusinessRules.Count);

                var result = new DomainValidationResult
                {
                    Success = true,
                    ValidatedAt = DateTime.UtcNow
                };

                // Validate entities
                var entityValidationResult = await _entityValidator.ValidateEntitiesAsync(domainLogic.Entities, cancellationToken);
                if (!entityValidationResult.Success)
                {
                    result.Issues.AddRange(entityValidationResult.Issues);
                    result.Warnings.AddRange(entityValidationResult.Warnings);
                    result.Suggestions.AddRange(entityValidationResult.Suggestions);
                }

                // Validate value objects
                var valueObjectValidationResult = await _valueObjectValidator.ValidateValueObjectsAsync(domainLogic.ValueObjects, cancellationToken);
                if (!valueObjectValidationResult.Success)
                {
                    result.Issues.AddRange(valueObjectValidationResult.Issues);
                    result.Warnings.AddRange(valueObjectValidationResult.Warnings);
                    result.Suggestions.AddRange(valueObjectValidationResult.Suggestions);
                }

                // Validate business rules
                var businessRuleValidationResult = await _businessRuleValidator.ValidateBusinessRulesAsync(domainLogic.BusinessRules, cancellationToken);
                if (!businessRuleValidationResult.Success)
                {
                    result.Issues.AddRange(businessRuleValidationResult.Issues);
                    result.Warnings.AddRange(businessRuleValidationResult.Warnings);
                    result.Suggestions.AddRange(businessRuleValidationResult.Suggestions);
                }

                // Validate services
                var serviceValidationResult = await _serviceValidator.ValidateServicesAsync(domainLogic.Services, cancellationToken);
                if (!serviceValidationResult.Success)
                {
                    result.Issues.AddRange(serviceValidationResult.Issues);
                    result.Warnings.AddRange(serviceValidationResult.Warnings);
                    result.Suggestions.AddRange(serviceValidationResult.Suggestions);
                }

                result.Success = result.Issues.Count == 0;
                result.Message = result.Success ? "Domain logic validation completed successfully" : $"Domain logic validation completed with {result.Issues.Count} issues";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during domain logic validation");
                return new DomainValidationResult
                {
                    Success = false,
                    Message = $"Domain logic validation failed: {ex.Message}",
                    ValidatedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Validates business rules by delegating to business rule validator
        /// </summary>
        public async Task<BusinessRuleValidationResult> ValidateBusinessRulesAsync(List<BusinessRule> rules, CancellationToken cancellationToken = default)
        {
            return await _businessRuleValidator.ValidateBusinessRulesAsync(rules, cancellationToken);
        }

        /// <summary>
        /// Checks consistency by delegating to consistency checker
        /// </summary>
        public async Task<ConsistencyCheckResult> CheckConsistencyAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
        {
            return await _consistencyChecker.CheckConsistencyAsync(domainLogic, cancellationToken);
        }

        /// <summary>
        /// Optimizes domain logic by delegating to optimization analyzer
        /// </summary>
        public async Task<OptimizationResult> OptimizeDomainLogicAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
        {
            return await _optimizationAnalyzer.OptimizeDomainLogicAsync(domainLogic, cancellationToken);
        }

        /// <summary>
        /// Validates entities by delegating to entity validator
        /// </summary>
        public async Task<EntityValidationResult> ValidateEntitiesAsync(List<string> entities, CancellationToken cancellationToken = default)
        {
            return await _entityValidator.ValidateEntitiesAsync(entities, cancellationToken);
        }

        /// <summary>
        /// Validates value objects by delegating to value object validator
        /// </summary>
        public async Task<ValueObjectValidationResult> ValidateValueObjectsAsync(List<string> valueObjects, CancellationToken cancellationToken = default)
        {
            return await _valueObjectValidator.ValidateValueObjectsAsync(valueObjects, cancellationToken);
        }

        /// <summary>
        /// Validates services by delegating to service validator
        /// </summary>
        public async Task<ServiceValidationResult> ValidateServicesAsync(List<string> services, CancellationToken cancellationToken = default)
        {
            return await _serviceValidator.ValidateServicesAsync(services, cancellationToken);
        }

        /// <summary>
        /// Generates validation report by delegating to report generator
        /// </summary>
        public async Task<ValidationReport> GenerateValidationReportAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default)
        {
            return await _reportGenerator.GenerateValidationReportAsync(domainLogic, cancellationToken);
        }
    }
}
