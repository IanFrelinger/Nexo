using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Validation
{
    /// <summary>
    /// Interface for validating generated domain logic
    /// </summary>
    public interface IDomainLogicValidator
    {
        /// <summary>
        /// Validates complete domain logic
        /// </summary>
        Task<DomainValidationResult> ValidateDomainLogicAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates business rules for consistency
        /// </summary>
        Task<BusinessRuleValidationResult> ValidateBusinessRulesAsync(List<BusinessRule> rules, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks consistency across domain logic components
        /// </summary>
        Task<ConsistencyCheckResult> CheckConsistencyAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes domain logic for performance and maintainability
        /// </summary>
        Task<OptimizationResult> OptimizeDomainLogicAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates domain entities for correctness
        /// </summary>
        Task<EntityValidationResult> ValidateEntitiesAsync(List<DomainEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates value objects for correctness
        /// </summary>
        Task<ValueObjectValidationResult> ValidateValueObjectsAsync(List<ValueObject> valueObjects, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates domain services for correctness
        /// </summary>
        Task<ServiceValidationResult> ValidateServicesAsync(List<DomainService> services, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates validation report for domain logic
        /// </summary>
        Task<ValidationReport> GenerateValidationReportAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of domain logic validation
    /// </summary>
    public class DomainValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<ValidationIssue> Issues { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public List<ValidationSuggestion> Suggestions { get; set; } = new();
        public ValidationScore Score { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of business rule validation
    /// </summary>
    public class BusinessRuleValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<BusinessRuleIssue> Issues { get; set; } = new();
        public List<BusinessRuleWarning> Warnings { get; set; } = new();
        public List<BusinessRuleSuggestion> Suggestions { get; set; } = new();
        public BusinessRuleScore Score { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of consistency check
    /// </summary>
    public class ConsistencyCheckResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<ConsistencyIssue> Issues { get; set; } = new();
        public List<ConsistencyWarning> Warnings { get; set; } = new();
        public List<ConsistencySuggestion> Suggestions { get; set; } = new();
        public ConsistencyScore Score { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of domain logic optimization
    /// </summary>
    public class OptimizationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<OptimizationSuggestion> Suggestions { get; set; } = new();
        public List<OptimizationImprovement> Improvements { get; set; } = new();
        public OptimizationScore Score { get; set; } = new();
        public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of entity validation
    /// </summary>
    public class EntityValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<EntityIssue> Issues { get; set; } = new();
        public List<EntityWarning> Warnings { get; set; } = new();
        public List<EntitySuggestion> Suggestions { get; set; } = new();
        public EntityScore Score { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of value object validation
    /// </summary>
    public class ValueObjectValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<ValueObjectIssue> Issues { get; set; } = new();
        public List<ValueObjectWarning> Warnings { get; set; } = new();
        public List<ValueObjectSuggestion> Suggestions { get; set; } = new();
        public ValueObjectScore Score { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of service validation
    /// </summary>
    public class ServiceValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<ServiceIssue> Issues { get; set; } = new();
        public List<ServiceWarning> Warnings { get; set; } = new();
        public List<ServiceSuggestion> Suggestions { get; set; } = new();
        public ServiceScore Score { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Validation report for domain logic
    /// </summary>
    public class ValidationReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DomainValidationResult DomainValidation { get; set; } = new();
        public BusinessRuleValidationResult BusinessRuleValidation { get; set; } = new();
        public ConsistencyCheckResult ConsistencyCheck { get; set; } = new();
        public OptimizationResult Optimization { get; set; } = new();
        public OverallScore OverallScore { get; set; } = new();
        public List<ValidationRecommendation> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Validation issue classes

    public class ValidationIssue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        public string Location { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ValidationWarning
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ValidationSuggestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Implementation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Specific validation issue classes

    public class BusinessRuleIssue : ValidationIssue { }
    public class BusinessRuleWarning : ValidationWarning { }
    public class BusinessRuleSuggestion : ValidationSuggestion { }

    public class ConsistencyIssue : ValidationIssue { }
    public class ConsistencyWarning : ValidationWarning { }
    public class ConsistencySuggestion : ValidationSuggestion { }

    public class OptimizationSuggestion : ValidationSuggestion { }
    public class OptimizationImprovement : ValidationSuggestion { }

    public class EntityIssue : ValidationIssue { }
    public class EntityWarning : ValidationWarning { }
    public class EntitySuggestion : ValidationSuggestion { }

    public class ValueObjectIssue : ValidationIssue { }
    public class ValueObjectWarning : ValidationWarning { }
    public class ValueObjectSuggestion : ValidationSuggestion { }

    public class ServiceIssue : ValidationIssue { }
    public class ServiceWarning : ValidationWarning { }
    public class ServiceSuggestion : ValidationSuggestion { }

    // Score classes

    public class ValidationScore
    {
        public double Overall { get; set; }
        public double Correctness { get; set; }
        public double Completeness { get; set; }
        public double Consistency { get; set; }
        public double Maintainability { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    public class BusinessRuleScore : ValidationScore { }
    public class ConsistencyScore : ValidationScore { }
    public class OptimizationScore : ValidationScore { }
    public class EntityScore : ValidationScore { }
    public class ValueObjectScore : ValidationScore { }
    public class ServiceScore : ValidationScore { }

    public class OverallScore
    {
        public double Overall { get; set; }
        public double DomainLogic { get; set; }
        public double BusinessRules { get; set; }
        public double Consistency { get; set; }
        public double Optimization { get; set; }
        public double Entities { get; set; }
        public double ValueObjects { get; set; }
        public double Services { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ValidationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Implementation { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
