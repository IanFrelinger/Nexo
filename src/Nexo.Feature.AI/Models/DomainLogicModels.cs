using System;
using System.Collections.Generic;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Context for domain logic generation.
    /// </summary>
    public class DomainLogicContext
    {
        public string Domain { get; set; }
        public string Industry { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<string> Constraints { get; set; } = new List<string>();
        public List<string> Patterns { get; set; } = new List<string>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of domain logic generation.
    /// </summary>
    public class DomainLogicResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DomainLogic GeneratedLogic { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
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
        public List<DomainEntity> Entities { get; set; } = new List<DomainEntity>();
        public List<ValueObject> ValueObjects { get; set; } = new List<ValueObject>();
        public List<DomainService> Services { get; set; } = new List<DomainService>();
        public List<DomainEvent> Events { get; set; } = new List<DomainEvent>();
        public List<BusinessRule> BusinessRules { get; set; } = new List<BusinessRule>();
        public List<DomainAggregate> Aggregates { get; set; } = new List<DomainAggregate>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a domain entity.
    /// </summary>
    public class DomainEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<EntityProperty> Properties { get; set; } = new List<EntityProperty>();
        public List<EntityMethod> Methods { get; set; } = new List<EntityMethod>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public EntityType Type { get; set; }
        public bool IsAggregateRoot { get; set; }
        public List<BusinessRule> Invariants { get; set; } = new List<BusinessRule>();
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents an entity property.
    /// </summary>
    public class EntityProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public string DefaultValue { get; set; }
        public List<ValidationRule> Validations { get; set; } = new List<ValidationRule>();
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents an entity method.
    /// </summary>
    public class EntityMethod
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<MethodParameter> Parameters { get; set; } = new List<MethodParameter>();
        public string Description { get; set; }
        public MethodType Type { get; set; }
        public List<BusinessRule> Preconditions { get; set; } = new List<BusinessRule>();
        public List<BusinessRule> Postconditions { get; set; } = new List<BusinessRule>();
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a method parameter.
    /// </summary>
    public class MethodParameter
    {
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
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ValueObjectProperty> Properties { get; set; } = new List<ValueObjectProperty>();
        public List<ValueObjectMethod> Methods { get; set; } = new List<ValueObjectMethod>();
        public bool IsImmutable { get; set; } = true;
        public List<ValidationRule> Validations { get; set; } = new List<ValidationRule>();
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
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<MethodParameter> Parameters { get; set; } = new List<MethodParameter>();
        public string Description { get; set; }
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a domain service.
    /// </summary>
    public class DomainService
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ServiceMethod> Methods { get; set; } = new List<ServiceMethod>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public ServiceType Type { get; set; }
        public List<BusinessRule> BusinessRules { get; set; } = new List<BusinessRule>();
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a service method.
    /// </summary>
    public class ServiceMethod
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<MethodParameter> Parameters { get; set; } = new List<MethodParameter>();
        public string Description { get; set; }
        public List<BusinessRule> Preconditions { get; set; } = new List<BusinessRule>();
        public List<BusinessRule> Postconditions { get; set; } = new List<BusinessRule>();
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a domain event.
    /// </summary>
    public class DomainEvent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<EventProperty> Properties { get; set; } = new List<EventProperty>();
        public EventType Type { get; set; }
        public List<string> Handlers { get; set; } = new List<string>();
        public bool IsAsync { get; set; }
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents an event property.
    /// </summary>
    public class EventProperty
    {
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
        public string Name { get; set; }
        public string Description { get; set; }
        public DomainEntity RootEntity { get; set; }
        public List<DomainEntity> ChildEntities { get; set; } = new List<DomainEntity>();
        public List<DomainEvent> Events { get; set; } = new List<DomainEvent>();
        public List<BusinessRule> Invariants { get; set; } = new List<BusinessRule>();
        public string GeneratedCode { get; set; }
    }

    /// <summary>
    /// Represents a validation rule.
    /// </summary>
    public class ValidationRule
    {
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
        public List<BusinessRule> ExtractedRules { get; set; } = new List<BusinessRule>();
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
        public List<DomainEntity> GeneratedEntities { get; set; } = new List<DomainEntity>();
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
        public List<ValueObject> GeneratedValueObjects { get; set; } = new List<ValueObject>();
        public double ConfidenceScore { get; set; }
        public ProcessingMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Result of domain service generation.
    /// </summary>
    public class DomainServiceResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<DomainService> GeneratedServices { get; set; } = new List<DomainService>();
        public double ConfidenceScore { get; set; }
        public ProcessingMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Result of domain event generation.
    /// </summary>
    public class DomainEventResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<DomainEvent> GeneratedEvents { get; set; } = new List<DomainEvent>();
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
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
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
        public List<FeatureRequirement> Requirements { get; set; } = new List<FeatureRequirement>();

        /// <summary>
        /// Processing metadata
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Options for domain logic optimization
    /// </summary>
    public class DomainLogicOptimizationOptions
    {
        public bool OptimizePerformance { get; set; } = true;
        public bool OptimizeMemory { get; set; } = true;
        public bool OptimizeReadability { get; set; } = true;
        public bool OptimizeMaintainability { get; set; } = true;
        public bool ApplyDesignPatterns { get; set; } = true;
        public bool OptimizeNamingConventions { get; set; } = true;
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of domain logic optimization
    /// </summary>
    public class DomainLogicOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DomainLogic OptimizedLogic { get; set; } = new DomainLogic();
        public List<string> Suggestions { get; set; } = new List<string>();
        public double OptimizationScore { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Test Suite Generation Models

    /// <summary>
    /// Result of unit test suite generation
    /// </summary>
    public class UnitTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<UnitTest> Tests { get; set; } = new List<UnitTest>();
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
        public List<string> TestCases { get; set; } = new List<string>();
        public string ExpectedBehavior { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of integration test suite generation
    /// </summary>
    public class IntegrationTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<IntegrationTest> Tests { get; set; } = new List<IntegrationTest>();
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
        public List<string> Components { get; set; } = new List<string>();
        public string TestScenario { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of edge case test suite generation
    /// </summary>
    public class EdgeCaseTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<EdgeCaseTest> Tests { get; set; } = new List<EdgeCaseTest>();
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
        public List<UnitTest> UnitTests { get; set; } = new List<UnitTest>();
        public List<IntegrationTest> IntegrationTests { get; set; } = new List<IntegrationTest>();
        public List<EdgeCaseTest> EdgeCaseTests { get; set; } = new List<EdgeCaseTest>();
        public int TotalTestCount { get; set; }
        public double OverallCoverage { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of complete test suite generation
    /// </summary>
    public class CompleteTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public CompleteTestSuite TestSuite { get; set; } = new CompleteTestSuite();
        public TestCoverageValidationResult CoverageValidation { get; set; } = new TestCoverageValidationResult();
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
        public List<string> ValidationMessages { get; set; } = new List<string>();
        public List<string> UncoveredAreas { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public bool MeetsThreshold { get; set; }
        public double CoverageThreshold { get; set; } = 90.0;
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of performance validation
    /// </summary>
    public class PerformanceValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double PerformanceScore { get; set; }
        public List<string> PerformanceIssues { get; set; } = new List<string>();
        public List<string> PerformanceRecommendations { get; set; } = new List<string>();
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new Dictionary<string, double>();
        public bool MeetsPerformanceThreshold { get; set; }
        public double PerformanceThreshold { get; set; } = 0.8;
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of security validation
    /// </summary>
    public class SecurityValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double SecurityScore { get; set; }
        public List<string> SecurityIssues { get; set; } = new List<string>();
        public List<string> SecurityRecommendations { get; set; } = new List<string>();
        public List<string> Vulnerabilities { get; set; } = new List<string>();
        public List<string> SecurityBestPractices { get; set; } = new List<string>();
        public bool MeetsSecurityThreshold { get; set; }
        public double SecurityThreshold { get; set; } = 0.9;
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of architectural validation
    /// </summary>
    public class ArchitecturalValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double ArchitecturalScore { get; set; }
        public List<string> ArchitecturalIssues { get; set; } = new List<string>();
        public List<string> ArchitecturalRecommendations { get; set; } = new List<string>();
        public List<string> PatternViolations { get; set; } = new List<string>();
        public List<string> DesignPrinciples { get; set; } = new List<string>();
        public bool MeetsArchitecturalThreshold { get; set; }
        public double ArchitecturalThreshold { get; set; } = 0.85;
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of comprehensive validation including all validation types
    /// </summary>
    public class ComprehensiveValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public double OverallScore { get; set; }
        public DomainLogicValidationResult BasicValidation { get; set; } = new DomainLogicValidationResult();
        public PerformanceValidationResult PerformanceValidation { get; set; } = new PerformanceValidationResult();
        public SecurityValidationResult SecurityValidation { get; set; } = new SecurityValidationResult();
        public ArchitecturalValidationResult ArchitecturalValidation { get; set; } = new ArchitecturalValidationResult();
        public List<string> AllIssues { get; set; } = new List<string>();
        public List<string> AllRecommendations { get; set; } = new List<string>();
        public bool MeetsAllThresholds { get; set; }
        public string Summary { get; set; } = string.Empty;
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of test data generation
    /// </summary>
    public class TestDataSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<TestDataItem> TestData { get; set; } = new List<TestDataItem>();
        public int TotalDataItems { get; set; }
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance test generation
    /// </summary>
    public class PerformanceTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<PerformanceTest> Tests { get; set; } = new List<PerformanceTest>();
        public int TotalTests { get; set; }
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of security test generation
    /// </summary>
    public class SecurityTestSuiteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<SecurityTest> Tests { get; set; } = new List<SecurityTest>();
        public int TotalTests { get; set; }
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
        public List<string> Dependencies { get; set; } = new List<string>();
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
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of business rule validation
    /// </summary>
    public class BusinessRuleValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of entity validation
    /// </summary>
    public class EntityValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of value object validation
    /// </summary>
    public class ValueObjectValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of consistency validation
    /// </summary>
    public class ConsistencyValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Result of completeness validation
    /// </summary>
    public class CompletenessValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
        public double ValidationScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public double CompletenessPercentage { get; set; }
        public List<string> MissingComponents { get; set; } = new List<string>();
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }
}