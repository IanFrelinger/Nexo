using System.Collections.Generic;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Result of domain logic generation.
    /// </summary>
    public class DomainLogicResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DomainLogic GeneratedLogic { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Warnings { get; set; } = [];
        public List<string> Recommendations { get; set; } = [];
        public ProcessingMetadata Metadata { get; set; }

        public DomainLogicResult()
        {
            GeneratedLogic = new DomainLogic();
            Metadata = new ProcessingMetadata();
        }
    }

    /// <summary>
    /// Represents generated domain logic.
    /// </summary>
    public class DomainLogic
    {
        public List<DomainEntity> Entities { get; set; } = [];
        public List<ValueObject> ValueObjects { get; set; } = [];
        public List<DomainService> Services { get; set; } = [];
        public List<DomainEvent> Events { get; set; } = [];
        public List<BusinessRule> BusinessRules { get; set; } = [];
        public List<DomainAggregate> Aggregates { get; set; } = [];
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a domain entity.
    /// </summary>
    public class DomainEntity
    {
        private List<BusinessRule> _invariants = new List<BusinessRule>();
        public string Name { get; set; }
        public string Description { get; set; }
        public List<EntityProperty> Properties { get; set; } = new List<EntityProperty>();
        public List<EntityMethod> Methods { get; set; } = new List<EntityMethod>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public EntityType Type { get; set; }
        public bool IsAggregateRoot { get; set; }

        public List<BusinessRule> Invariants
        {
            get => _invariants;
            set => _invariants = value;
        }

        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents an entity property.
    /// </summary>
    public class EntityProperty
    {
        private List<ValidationRule> _validations = [];
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public string DefaultValue { get; set; }

        public List<ValidationRule> Validations
        {
            get => _validations;
            set => _validations = value;
        }

        public string Description { get; set; }
    }

    /// <summary>
    /// Represents an entity method.
    /// </summary>
    public class EntityMethod
    {
        private List<MethodParameter> _parameters = [];

        public EntityMethod(MethodType type, string generatedCode)
        {
            Type = type;
            GeneratedCode = generatedCode;
        }

        public string Name { get; set; }
        public string ReturnType { get; set; }

        public List<MethodParameter> Parameters
        {
            get => _parameters;
            set => _parameters = value;
        }

        public string Description { get; set; }
        public MethodType Type { get; set; }
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a method parameter.
    /// </summary>
    public class MethodParameter
    {
        public MethodParameter(bool isRequired, string description)
        {
            IsRequired = isRequired;
            Description = description;
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents a value object.
    /// </summary>
    public class ValueObject
    {
        private List<ValidationRule> _validations = [];
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ValueObjectProperty> Properties { get; set; } = [];
        public List<ValueObjectMethod> Methods { get; set; } = [];
        public bool IsImmutable { get; set; } = true;

        public List<ValidationRule> Validations
        {
            get
            {
                var validationRules = _validations;
                return validationRules;
            }
            set => _validations = value;
        }

        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a value object property.
    /// </summary>
    public class ValueObjectProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents a value object method.
    /// </summary>
    public class ValueObjectMethod
    {
        public ValueObjectMethod(string name, string returnType, string description, string generatedCode)
        {
            Name = name;
            ReturnType = returnType;
            Description = description;
            GeneratedCode = generatedCode;
        }

        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Description { get; set; }
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a domain service.
    /// </summary>
    public class DomainService
    {
        private List<ServiceMethod> _methods = [];
        public string Name { get; set; }
        public string Description { get; set; }

        public List<ServiceMethod> Methods
        {
            get => _methods;
            set => _methods = value;
        }

        public List<string> Dependencies { get; set; } = [];
        public ServiceType Type { get; set; }
        public List<BusinessRule> BusinessRules { get; set; } = [];
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a service method.
    /// </summary>
    public class ServiceMethod
    {
        public ServiceMethod(string returnType, string description, string generatedCode)
        {
            ReturnType = returnType;
            Description = description;
            GeneratedCode = generatedCode;
        }

        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Description { get; set; }
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a domain event.
    /// </summary>
    public class DomainEvent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<EventProperty> Properties { get; set; } = [];
        public EventType Type { get; set; }
        public List<string> Handlers { get; set; } = [];
        public bool IsAsync { get; set; }
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents an event property.
    /// </summary>
    public class EventProperty
    {
        public EventProperty(string name, string type, bool isRequired, string description)
        {
            Name = name;
            Type = type;
            IsRequired = isRequired;
            Description = description;
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents a domain aggregate.
    /// </summary>
    public class DomainAggregate
    {
        public DomainAggregate(string name, string description, DomainEntity rootEntity, string generatedCode)
        {
            Name = name;
            Description = description;
            RootEntity = rootEntity;
            GeneratedCode = generatedCode;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public DomainEntity RootEntity { get; set; }
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a validation rule.
    /// </summary>
    public class ValidationRule
    {
        public ValidationRule(string expression, string errorMessage, ValidationSeverity severity)
        {
            Expression = expression;
            ErrorMessage = errorMessage;
            Severity = severity;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public ValidationType Type { get; set; }
        public string Expression { get; set; }
        public string ErrorMessage { get; set; }
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// Result of business rule extraction.
    /// </summary>
    public class BusinessRuleExtractionResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<BusinessRule> ExtractedRules { get; set; } = [];
        public double ConfidenceScore { get; set; }
        public ProcessingMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Result of domain entity generation.
    /// </summary>
    public class DomainEntityResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<DomainEntity> GeneratedEntities { get; set; } = [];
        public double ConfidenceScore { get; set; }
        public ProcessingMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Result of value object generation.
    /// </summary>
    public class ValueObjectResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValueObject> GeneratedValueObjects { get; set; } = [];
        public double ConfidenceScore { get; set; }
        public ProcessingMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Result of domain logic validation.
    /// </summary>
    public class DomainLogicValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = [];
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = [];
        public ProcessingMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Result of domain logic optimization.
    /// </summary>
    public class OptimizationSuggestion
    {
        public string Component { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Impact { get; set; }
        public double Priority { get; set; }
    }

    /// <summary>
    /// Represents processed requirements for domain logic generation.
    /// </summary>
    public class ProcessedRequirements
    {
        /// <summary>
        /// The list of feature requirements
        /// </summary>
        public List<FeatureRequirement> Requirements { get; set; } = [];
    }

    /// <summary>
    /// Options for domain logic optimization
    /// </summary>
    public class DomainLogicOptimizationOptions
    {
        private Dictionary<string, object> _customOptions = new();
        public bool OptimizePerformance { get; set; } = true;
        public bool OptimizeMemory { get; set; } = true;
        public bool OptimizeReadability { get; set; } = true;
        public bool OptimizeMaintainability { get; set; } = true;
        public bool ApplyDesignPatterns { get; set; } = true;
        public bool OptimizeNamingConventions { get; set; } = true;

        public Dictionary<string, object> CustomOptions
        {
            get => _customOptions;
            set => _customOptions = value;
        }
    }

    /// <summary>
    /// Result of domain logic optimization
    /// </summary>
    public class DomainLogicOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DomainLogic OptimizedLogic { get; set; } = new();
        public List<string> Suggestions { get; set; } = [];
        public double OptimizationScore { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Test Suite Generation Models

    /// <summary>
    /// Result of unit test suite generation
    /// </summary>
    public class UnitTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<UnitTest> Tests { get; set; } = [];
        public int TotalTests { get; set; }
        public double CoveragePercentage { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual unit test
    /// </summary>
    public class UnitTest
    {
        public string TestName { get; set; } = string.Empty;
        public string TestMethod { get; set; } = string.Empty;
        public string TestClass { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpectedBehavior { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of integration test suite generation
    /// </summary>
    public class IntegrationTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<IntegrationTest> Tests { get; set; } = [];
        public int TotalTests { get; set; }
        public double CoveragePercentage { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual integration test
    /// </summary>
    public class IntegrationTest
    {
        public string TestName { get; set; } = string.Empty;
        public string TestMethod { get; set; } = string.Empty;
        public string TestClass { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Components { get; set; } = [];
        public string TestScenario { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of edge case test suite generation
    /// </summary>
    public class EdgeCaseTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<EdgeCaseTest> Tests { get; set; } = [];
        public int TotalTests { get; set; }
        public double CoveragePercentage { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual edge case test
    /// </summary>
    public class EdgeCaseTest
    {
        public string TestName { get; set; } = string.Empty;
        public string TestMethod { get; set; } = string.Empty;
        public string TestClass { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EdgeCaseType { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public string ExpectedBehavior { get; set; } = string.Empty;
    }

    /// <summary>
    /// Complete test suite containing all test types
    /// </summary>
    public class CompleteTestSuite
    {
        public List<UnitTest> UnitTests { get; set; } = [];
        public List<IntegrationTest> IntegrationTests { get; set; } = [];
        public List<EdgeCaseTest> EdgeCaseTests { get; set; } = [];
        public int TotalTestCount { get; set; }
        public double OverallCoverage { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of a complete test suite generation
    /// </summary>
    public class CompleteTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public CompleteTestSuite TestSuite { get; set; } = new();
        public TestCoverageValidationResult CoverageValidation { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of test coverage validation
    /// </summary>
    public class TestCoverageValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double CoveragePercentage { get; set; }
        public List<string> ValidationMessages { get; set; } = [];
        public List<string> UncoveredAreas { get; set; } = [];
        public List<string> Recommendations { get; set; } = [];
        public bool MeetsThreshold { get; set; }
        public double CoverageThreshold { get; set; } = 90.0;
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of performance validation
    /// </summary>
    public class PerformanceValidationResult
    {
        public PerformanceValidationResult(double performanceThreshold, string errorMessage)
        {
            PerformanceThreshold = performanceThreshold;
            ErrorMessage = errorMessage;
        }

        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double PerformanceScore { get; set; }
        public List<string> PerformanceIssues { get; set; } = [];
        public List<string> PerformanceRecommendations { get; set; } = [];
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
        public bool MeetsPerformanceThreshold { get; set; }
        public double PerformanceThreshold { get; set; }
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of security validation
    /// </summary>
    public class SecurityValidationResult
    {
        public SecurityValidationResult(string errorMessage, double securityThreshold)
        {
            ErrorMessage = errorMessage;
            SecurityThreshold = securityThreshold;
        }

        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double SecurityScore { get; set; }
        public List<string> SecurityIssues { get; set; } = [];
        public List<string> SecurityRecommendations { get; set; } = [];
        public List<string> Vulnerabilities { get; set; } = [];
        public List<string> SecurityBestPractices { get; set; } = [];
        public bool MeetsSecurityThreshold { get; set; }
        public double SecurityThreshold { get; set; }
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of architectural validation
    /// </summary>
    public class ArchitecturalValidationResult
    {
        public ArchitecturalValidationResult(string errorMessage, double architecturalThreshold)
        {
            ErrorMessage = errorMessage;
            ArchitecturalThreshold = architecturalThreshold;
        }

        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double ArchitecturalScore { get; set; }
        public List<string> ArchitecturalIssues { get; set; } = [];
        public List<string> ArchitecturalRecommendations { get; set; } = [];
        public List<string> PatternViolations { get; set; } = [];
        public List<string> DesignPrinciples { get; set; } = [];
        public bool MeetsArchitecturalThreshold { get; set; }
        public double ArchitecturalThreshold { get; set; }
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of comprehensive validation including all validation types
    /// </summary>
    public class ComprehensiveValidationResult
    {
        public ComprehensiveValidationResult(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double OverallScore { get; set; }
        public DomainLogicValidationResult BasicValidation { get; set; } = new();
        public PerformanceValidationResult PerformanceValidation { get; set; } = new PerformanceValidationResult(0.8, "Default performance validation");
        public SecurityValidationResult SecurityValidation { get; set; } = new SecurityValidationResult("Default security validation", 0.8);
        public ArchitecturalValidationResult ArchitecturalValidation { get; set; } = new ArchitecturalValidationResult("Default architectural validation", 0.8);
        public List<string> AllIssues { get; set; } = [];
        public List<string> AllRecommendations { get; set; } = [];
        public bool MeetsAllThresholds { get; set; }
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of test data generation
    /// </summary>
    public class TestDataSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<TestDataItem> TestData { get; set; } = [];
        public int TotalDataItems { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual test data item
    /// </summary>
    public class TestDataItem
    {
        public string EntityName { get; set; } = string.Empty;
        public string DataName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string DataValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsValid { get; set; } = true;
        public string UseCase { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of performance test generation
    /// </summary>
    public class PerformanceTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<PerformanceTest> Tests { get; set; } = [];
        public int TotalTests { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual performance test
    /// </summary>
    public class PerformanceTest
    {
        public string TestName { get; set; } = string.Empty;
        public string TestMethod { get; set; } = string.Empty;
        public string TestClass { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformanceMetric { get; set; } = string.Empty;
        public double ExpectedThreshold { get; set; }
        public string LoadProfile { get; set; } = string.Empty;
        public string TestScenario { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of security test generation
    /// </summary>
    public class SecurityTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<SecurityTest> Tests { get; set; } = [];
        public int TotalTests { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual security test
    /// </summary>
    public class SecurityTest
    {
        public string TestName { get; set; } = string.Empty;
        public string TestMethod { get; set; } = string.Empty;
        public string TestClass { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SecurityVulnerability { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public string AttackVector { get; set; } = string.Empty;
        public string MitigationStrategy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of accessibility test generation
    /// </summary>
    public class AccessibilityTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<AccessibilityTest> Tests { get; set; } = new List<AccessibilityTest>();
        public int TotalTests { get; set; }
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Individual accessibility test
    /// </summary>
    public class AccessibilityTest
    {
        public string TestName { get; set; } = string.Empty;
        public string TestMethod { get; set; } = string.Empty;
        public string TestClass { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AccessibilityGuideline { get; set; } = string.Empty;
        public string ComplianceLevel { get; set; } = string.Empty;
        public string UserScenario { get; set; } = string.Empty;
        public string AssistiveTechnology { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of business rule validation
    /// </summary>
    public class BusinessRuleValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = [];
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = [];
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of entity validation
    /// </summary>
    public class EntityValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = [];
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = [];
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of value object validation
    /// </summary>
    public class ValueObjectValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = [];
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = [];
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of consistency validation
    /// </summary>
    public class ConsistencyValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = [];
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = [];
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of completeness validation
    /// </summary>
    public class CompletenessValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = [];
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = [];
        public double CompletenessPercentage { get; set; }
        public List<string> MissingComponents { get; set; } = [];
        public ProcessingMetadata Metadata { get; set; } = new();
    }
}