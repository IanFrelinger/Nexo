using System;
using System.Collections.Generic;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Result of processing natural language feature requirements.
    /// </summary>
    public class FeatureRequirementResult
    {
        /// <summary>
        /// Whether the processing was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Extracted feature requirements.
        /// </summary>
        public List<FeatureRequirement> Requirements { get; set; } = [];

        /// <summary>
        /// Processing confidence score (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Processing warnings and issues.
        /// </summary>
        public List<string> Warnings { get; set; } = [];

        /// <summary>
        /// Processing errors.
        /// </summary>
        public List<string> Errors { get; set; } = [];

        /// <summary>
        /// Processing metadata and context.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Individual feature requirement extracted from natural language.
    /// </summary>
    public class FeatureRequirement
    {
        public FeatureRequirement(RequirementStatus status)
        {
            Status = status;
        }

        /// <summary>
        /// Unique identifier for the requirement.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Title or name of the requirement.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the requirement.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type of requirement (functional, non-functional, etc.).
        /// </summary>
        public RequirementType Type { get; set; }

        /// <summary>
        /// Priority level of the requirement.
        /// </summary>
        public RequirementPriority Priority { get; set; }

        /// <summary>
        /// Business value score (0.0 to 1.0).
        /// </summary>
        public double BusinessValue { get; set; }

        /// <summary>
        /// Technical complexity score (0.0 to 1.0).
        /// </summary>
        public double TechnicalComplexity { get; set; }

        /// <summary>
        /// Completeness score (0.0 to 1.0) indicating how complete the requirement is.
        /// </summary>
        public double CompletenessScore { get; set; }

        /// <summary>
        /// Estimated effort in story points.
        /// </summary>
        public int EstimatedEffort { get; set; }

        /// <summary>
        /// User stories associated with this requirement.
        /// </summary>
        public List<UserStory> UserStories { get; set; } = [];

        /// <summary>
        /// Acceptance criteria for the requirement.
        /// </summary>
        public List<string> AcceptanceCriteria { get; set; } = [];

        /// <summary>
        /// Business rules associated with the requirement.
        /// </summary>
        public List<BusinessRule> BusinessRules { get; set; } = [];

        /// <summary>
        /// Technical requirements and constraints.
        /// </summary>
        public List<TechnicalRequirement> TechnicalRequirements { get; set; } = [];

        /// <summary>
        /// Integration points and dependencies.
        /// </summary>
        public List<IntegrationPoint> IntegrationPoints { get; set; } = [];

        /// <summary>
        /// Tags and categories for the requirement.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp.
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Status of the requirement.
        /// </summary>
        public RequirementStatus Status { get; set; }
    }

    /// <summary>
    /// User story associated with a feature requirement.
    /// </summary>
    public class UserStory
    {
        public UserStory(RequirementPriority priority, int estimatedEffort)
        {
            Priority = priority;
            EstimatedEffort = estimatedEffort;
        }

        /// <summary>
        /// User story in the format "As a [role], I want [feature], so that [benefit]".
        /// </summary>
        public string Story { get; set; } = string.Empty;

        /// <summary>
        /// Priority of this user story.
        /// </summary>
        public RequirementPriority Priority { get; set; }

        /// <summary>
        /// Estimated effort in story points.
        /// </summary>
        public int EstimatedEffort { get; set; }
    }

    /// <summary>
    /// Business rule associated with a feature requirement.
    /// </summary>
    public class BusinessRule
    {
        /// <summary>
        /// Unique identifier for the business rule.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name or title of the business rule.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the business rule.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Business rule logic or condition.
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Action to take when the condition is met.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Priority of the business rule.
        /// </summary>
        public RequirementPriority Priority { get; set; }

        /// <summary>
        /// Whether the rule is mandatory or optional.
        /// </summary>
        public bool IsMandatory { get; set; } = true;

        /// <summary>
        /// Category of the business rule.
        /// </summary>
        public BusinessRuleCategory Category { get; set; }

        /// <summary>
        /// Whether the rule is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Technical requirement associated with a feature requirement.
    /// </summary>
    public class TechnicalRequirement
    {
        public TechnicalRequirement(TechnicalRequirementType type, RequirementPriority priority, bool isMandatory)
        {
            Type = type;
            Priority = priority;
            IsMandatory = isMandatory;
        }

        /// <summary>
        /// Type of technical requirement.
        /// </summary>
        public TechnicalRequirementType Type { get; set; }

        /// <summary>
        /// Priority of the technical requirement.
        /// </summary>
        public RequirementPriority Priority { get; set; }

        /// <summary>
        /// Whether the requirement is mandatory or optional.
        /// </summary>
        public bool IsMandatory { get; set; }
    }

    /// <summary>
    /// Integration point associated with a feature requirement.
    /// </summary>
    public class IntegrationPoint
    {
        public IntegrationPoint(IntegrationType type, bool isRequired)
        {
            Type = type;
            IsRequired = isRequired;
        }

        /// <summary>
        /// Type of integration (API, database, service, etc.).
        /// </summary>
        public IntegrationType Type { get; set; }

        /// <summary>
        /// Whether the integration is required or optional.
        /// </summary>
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// Result of validation operations.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Overall validation score (0.0 to 1.0).
        /// </summary>
        public double ValidationScore { get; set; }

        /// <summary>
        /// Completeness score (0.0 to 1.0).
        /// </summary>
        public double CompletenessScore { get; set; }

        /// <summary>
        /// Clarity score (0.0 to 1.0).
        /// </summary>
        public double ClarityScore { get; set; }

        /// <summary>
        /// Ambiguity score (0.0 to 1.0, lower is better).
        /// </summary>
        public double AmbiguityScore { get; set; }

        /// <summary>
        /// Validation issues found.
        /// </summary>
        public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();

        /// <summary>
        /// Recommendations for improvement.
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Individual validation issue.
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>
        /// Type of validation issue.
        /// </summary>
        public ValidationIssueType Type { get; set; }

        /// <summary>
        /// Severity of the issue.
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Description of the issue.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Location or context where the issue was found.
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Suggested fix for the issue.
        /// </summary>
        public string SuggestedFix { get; set; } = string.Empty;

        /// <summary>
        /// Component where the issue was found.
        /// </summary>
        public string Component { get; set; } = string.Empty;

        /// <summary>
        /// Property that has the issue.
        /// </summary>
        public string Property { get; set; } = string.Empty;

        /// <summary>
        /// Message describing the issue.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Scope of the issue.
        /// </summary>
        public ValidationScope Scope { get; set; }

        /// <summary>
        /// Suggestion for fixing the issue.
        /// </summary>
        public string Suggestion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of extraction operations.
    /// </summary>
    public class ExtractionResult
    {
        public ExtractionResult(ExtractionType extractionType)
        {
            ExtractionType = extractionType;
        }

        /// <summary>
        /// Whether the extraction was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Type of extraction performed.
        /// </summary>
        public ExtractionType ExtractionType { get; set; }

        /// <summary>
        /// Extracted components.
        /// </summary>
        public List<ExtractedComponent> Components { get; set; } = new List<ExtractedComponent>();

        /// <summary>
        /// Extraction confidence score (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Individual extracted component.
    /// </summary>
    public class ExtractedComponent
    {
        public ExtractedComponent(double confidenceScore)
        {
            ConfidenceScore = confidenceScore;
        }

        /// <summary>
        /// Confidence score for this component (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Result of domain terminology processing.
    /// </summary>
    public class DomainTerminologyResult
    {
        /// <summary>
        /// Whether the processing was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Recognized domain terms.
        /// </summary>
        public List<DomainTerm> RecognizedTerms { get; set; } = [];

        /// <summary>
        /// Unrecognized terms that may need attention.
        /// </summary>
        public List<string> UnrecognizedTerms { get; set; } = [];
    }

    /// <summary>
    /// Domain-specific term.
    /// </summary>
    public class DomainTerm
    {
        public DomainTerm(double confidenceScore)
        {
            ConfidenceScore = confidenceScore;
        }

        /// <summary>
        /// Confidence score for the term recognition (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Result of prioritization operations.
    /// </summary>
    public class PrioritizationResult
    {
        /// <summary>
        /// Whether the prioritization was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Categorized and prioritized requirements.
        /// </summary>
        public List<PrioritizedRequirement> PrioritizedRequirements { get; set; } = [];

        /// <summary>
        /// Categories used for organization.
        /// </summary>
        public List<RequirementCategory> Categories { get; set; } = [];

        /// <summary>
        /// Prioritization metrics and scores.
        /// </summary>
        public PrioritizationMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Requirement with prioritization information.
    /// </summary>
    public class PrioritizedRequirement
    {
        /// <summary>
        /// The feature requirement.
        /// </summary>
        public FeatureRequirement Requirement { get; set; } = new FeatureRequirement();

        /// <summary>
        /// Priority score (0.0 to 1.0).
        /// </summary>
        public double PriorityScore { get; set; }

        /// <summary>
        /// Business value score (0.0 to 1.0).
        /// </summary>
        public double BusinessValueScore { get; set; }

        /// <summary>
        /// Technical complexity score (0.0 to 1.0).
        /// </summary>
        public double TechnicalComplexityScore { get; set; }

        /// <summary>
        /// Risk score (0.0 to 1.0).
        /// </summary>
        public double RiskScore { get; set; }

        /// <summary>
        /// Category the requirement belongs to.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Recommended implementation order.
        /// </summary>
        public int ImplementationOrder { get; set; }
    }

    /// <summary>
    /// Category for organizing requirements.
    /// </summary>
    public class RequirementCategory
    {
        /// <summary>
        /// Name of the category.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the category.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Number of requirements in this category.
        /// </summary>
        public int RequirementCount { get; set; }

        /// <summary>
        /// Total estimated effort for this category.
        /// </summary>
        public int TotalEffort { get; set; }

        /// <summary>
        /// Average priority score for this category.
        /// </summary>
        public double AveragePriorityScore { get; set; }
    }

    /// <summary>
    /// Metrics for prioritization analysis.
    /// </summary>
    public class PrioritizationMetrics
    {
        /// <summary>
        /// Total number of requirements analyzed.
        /// </summary>
        public int TotalRequirements { get; set; }

        /// <summary>
        /// Number of high-priority requirements.
        /// </summary>
        public int HighPriorityCount { get; set; }

        /// <summary>
        /// Number of medium-priority requirements.
        /// </summary>
        public int MediumPriorityCount { get; set; }

        /// <summary>
        /// Number of low-priority requirements.
        /// </summary>
        public int LowPriorityCount { get; set; }

        /// <summary>
        /// Total estimated effort across all requirements.
        /// </summary>
        public int TotalEstimatedEffort { get; set; }

        /// <summary>
        /// Average business value score.
        /// </summary>
        public double AverageBusinessValue { get; set; }

        /// <summary>
        /// Average technical complexity score.
        /// </summary>
        public double AverageTechnicalComplexity { get; set; }
    }

    /// <summary>
    /// Processing metadata and context.
    /// </summary>
    public class ProcessingMetadata
    {
        /// <summary>
        /// Processing timestamp.
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Processing duration in milliseconds.
        /// </summary>
        public long ProcessingDurationMs { get; set; }

        /// <summary>
        /// Input format used.
        /// </summary>
        public InputFormat InputFormat { get; set; }

        /// <summary>
        /// Domain context used for processing.
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Processing model or algorithm used.
        /// </summary>
        public string ProcessingModel { get; set; } = string.Empty;

        /// <summary>
        /// Version of the processing logic.
        /// </summary>
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of feature requirements.
    /// </summary>
    public enum RequirementType
    {
        /// <summary>
        /// Functional requirement.
        /// </summary>
        Functional,

        /// <summary>
        /// Security requirement.
        /// </summary>
        Security,

        /// <summary>
        /// Performance requirement.
        /// </summary>
        Performance
    }

    /// <summary>
    /// Priority levels for requirements.
    /// </summary>
    public enum RequirementPriority
    {
        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Medium priority.
        /// </summary>
        Medium,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low
    }

    /// <summary>
    /// Types of technical requirements.
    /// </summary>
    public enum TechnicalRequirementType
    {
    }

    /// <summary>
    /// Types of integration.
    /// </summary>
    public enum IntegrationType
    {
    }

    /// <summary>
    /// Types of validation issues.
    /// </summary>
    public enum ValidationIssueType
    {
        /// <summary>
        /// Missing required field.
        /// </summary>
        MissingRequiredField,

        /// <summary>
        /// Incomplete information.
        /// </summary>
        IncompleteInformation,

        /// <summary>
        /// Inconsistent terminology.
        /// </summary>
        InconsistentTerminology,

        /// <summary>
        /// Missing business rules.
        /// </summary>
        MissingBusinessRules
    }

    /// <summary>
    /// Severity levels for validation issues.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// Critical severity.
        /// </summary>
        Critical,

        /// <summary>
        /// High severity.
        /// </summary>
        High,

        /// <summary>
        /// Medium severity.
        /// </summary>
        Medium,

        /// <summary>
        /// Low severity.
        /// </summary>
        Low,

        /// <summary>
        /// Information only.
        /// </summary>
        Info
    }

    // Domain Context Models for Story 5.1.3: Domain Context Understanding

    /// <summary>
    /// Result of domain context processing.
    /// </summary>
    public class DomainContextResult
    {
        /// <summary>
        /// Whether the processing was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Processed input with domain context applied.
        /// </summary>
        public string ProcessedInput { get; set; } = string.Empty;

        /// <summary>
        /// Extracted domain context information.
        /// </summary>
        public DomainContext DomainContext { get; set; } = new DomainContext();

        /// <summary>
        /// Domain-specific insights and analysis.
        /// </summary>
        public List<DomainInsight> Insights { get; set; } = new List<DomainInsight>();

        /// <summary>
        /// Confidence score for domain context understanding (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Domain-specific recommendations.
        /// </summary>
        public List<string> Recommendations { get; set; } = [];

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Domain context information.
    /// </summary>
    public class DomainContext
    {
        /// <summary>
        /// The target domain (e.g., "E-commerce", "Healthcare", "Finance").
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Industry classification.
        /// </summary>
        public string Industry { get; set; } = string.Empty;

        /// <summary>
        /// Business context and background.
        /// </summary>
        public string BusinessContext { get; set; } = string.Empty;

        /// <summary>
        /// Key stakeholders and roles.
        /// </summary>
        public List<string> Stakeholders { get; set; } = new List<string>();

        /// <summary>
        /// Regulatory and compliance requirements.
        /// </summary>
        public List<string> ComplianceRequirements { get; set; } = new List<string>();

        /// <summary>
        /// Business processes and workflows.
        /// </summary>
        public List<string> BusinessProcesses { get; set; } = new List<string>();

        /// <summary>
        /// Technical constraints and limitations.
        /// </summary>
        public List<string> TechnicalConstraints { get; set; } = new List<string>();

        /// <summary>
        /// Domain-specific rules and policies.
        /// </summary>
        public List<DomainRule> DomainRules { get; set; } = new List<DomainRule>();
    }

    /// <summary>
    /// Domain-specific insight.
    /// </summary>
    public class DomainInsight
    {
        /// <summary>
        /// Type of insight.
        /// </summary>
        public InsightType Type { get; set; }

        /// <summary>
        /// Insight description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score for the insight (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Impact level of the insight.
        /// </summary>
        public ImpactLevel Impact { get; set; }

        /// <summary>
        /// Related domain terms or concepts.
        /// </summary>
        public List<string> RelatedConcepts { get; set; } = [];
    }

    /// <summary>
    /// Domain-specific rule.
    /// </summary>
    public class DomainRule
    {
        /// <summary>
        /// Rule identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Rule name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rule description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Rule condition.
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Rule action or consequence.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Rule priority.
        /// </summary>
        public RequirementPriority Priority { get; set; }

        /// <summary>
        /// Whether the rule is mandatory.
        /// </summary>
        public bool IsMandatory { get; set; } = true;
    }

    /// <summary>
    /// Result of business terminology recognition.
    /// </summary>
    public class BusinessTerminologyResult
    {
        /// <summary>
        /// Whether the recognition was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Recognized business terms.
        /// </summary>
        public List<BusinessTerm> RecognizedTerms { get; set; } = new List<BusinessTerm>();

        /// <summary>
        /// Unrecognized terms that may need attention.
        /// </summary>
        public List<string> UnrecognizedTerms { get; set; } = new List<string>();

        /// <summary>
        /// Business terminology suggestions.
        /// </summary>
        public List<string> Suggestions { get; set; } = [];

        /// <summary>
        /// Confidence score for terminology recognition (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Business-specific term.
    /// </summary>
    public class BusinessTerm
    {
        /// <summary>
        /// The business term.
        /// </summary>
        public string Term { get; set; } = string.Empty;

        /// <summary>
        /// Business definition of the term.
        /// </summary>
        public string Definition { get; set; } = string.Empty;

        /// <summary>
        /// Business category or domain.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Business context where the term is used.
        /// </summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score for term recognition (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Business synonyms or related terms.
        /// </summary>
        public List<string> Synonyms { get; set; } = [];

        /// <summary>
        /// Business rules associated with the term.
        /// </summary>
        public List<string> AssociatedRules { get; set; } = [];
    }

    /// <summary>
    /// Result of industry pattern recognition.
    /// </summary>
    public class IndustryPatternResult
    {
        /// <summary>
        /// Whether the pattern recognition was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Identified industry patterns.
        /// </summary>
        public List<IndustryPattern> IdentifiedPatterns { get; set; } = [];

        /// <summary>
        /// Industry-specific recommendations.
        /// </summary>
        public List<string> Recommendations { get; set; } = [];

        /// <summary>
        /// Confidence score for pattern recognition (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Industry-specific pattern.
    /// </summary>
    public class IndustryPattern
    {
        /// <summary>
        /// Pattern identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Pattern name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Pattern description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Industry where the pattern is commonly used.
        /// </summary>
        public string Industry { get; set; } = string.Empty;

        /// <summary>
        /// Pattern category.
        /// </summary>
        public PatternCategory Category { get; set; }

        /// <summary>
        /// Pattern confidence score (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Pattern implementation guidelines.
        /// </summary>
        public List<string> Guidelines { get; set; } = [];

        /// <summary>
        /// Related patterns.
        /// </summary>
        public List<string> RelatedPatterns { get; set; } = [];
    }

    /// <summary>
    /// Result of domain knowledge integration.
    /// </summary>
    public class DomainKnowledgeResult
    {
        /// <summary>
        /// Whether the integration was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Enhanced input with domain knowledge.
        /// </summary>
        public string EnhancedInput { get; set; } = string.Empty;

        /// <summary>
        /// Applied domain knowledge.
        /// </summary>
        public List<DomainKnowledge> AppliedKnowledge { get; set; } = [];

        /// <summary>
        /// Knowledge gaps identified.
        /// </summary>
        public List<string> KnowledgeGaps { get; set; } = [];

        /// <summary>
        /// Confidence score for knowledge integration (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Domain knowledge information.
    /// </summary>
    public class DomainKnowledge
    {
        /// <summary>
        /// Knowledge identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Knowledge title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Knowledge content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Knowledge category.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Knowledge source.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score for the knowledge (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Related concepts or terms.
        /// </summary>
        public List<string> RelatedConcepts { get; set; } = [];
    }

    /// <summary>
    /// Result of domain requirement validation.
    /// </summary>
    public class DomainValidationResult
    {
        /// <summary>
        /// Whether the validation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Whether all requirements are valid for the domain.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation issues found.
        /// </summary>
        public List<DomainValidationIssue> Issues { get; set; } = [];

        /// <summary>
        /// Domain-specific recommendations.
        /// </summary>
        public List<string> Recommendations { get; set; } = [];

        /// <summary>
        /// Overall validation score (0.0 to 1.0).
        /// </summary>
        public double ValidationScore { get; set; }

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Domain-specific validation issue.
    /// </summary>
    public class DomainValidationIssue
    {
        /// <summary>
        /// Issue identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Issue type.
        /// </summary>
        public DomainValidationIssueType Type { get; set; }

        /// <summary>
        /// Issue severity.
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Issue description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Affected requirement ID.
        /// </summary>
        public string RequirementId { get; set; } = string.Empty;

        /// <summary>
        /// Suggested fix for the issue.
        /// </summary>
        public string SuggestedFix { get; set; } = string.Empty;

        /// <summary>
        /// Domain rule that was violated.
        /// </summary>
        public string ViolatedRule { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of domain improvement suggestions.
    /// </summary>
    public class DomainImprovementResult
    {
        /// <summary>
        /// Whether the improvement analysis was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Improvement suggestions for requirements.
        /// </summary>
        public List<DomainImprovement> Improvements { get; set; } = [];

        /// <summary>
        /// Overall improvement score (0.0 to 1.0).
        /// </summary>
        public double ImprovementScore { get; set; }

        /// <summary>
        /// Domain-specific best practices.
        /// </summary>
        public List<string> BestPractices { get; set; } = [];

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Domain-specific improvement suggestion.
    /// </summary>
    public class DomainImprovement
    {
        /// <summary>
        /// Improvement identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Improvement type.
        /// </summary>
        public ImprovementType Type { get; set; }

        /// <summary>
        /// Improvement description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Affected requirement ID.
        /// </summary>
        public string RequirementId { get; set; } = string.Empty;

        /// <summary>
        /// Improvement priority.
        /// </summary>
        public RequirementPriority Priority { get; set; }

        /// <summary>
        /// Expected impact of the improvement.
        /// </summary>
        public ImpactLevel Impact { get; set; }

        /// <summary>
        /// Implementation guidance.
        /// </summary>
        public string ImplementationGuidance { get; set; } = string.Empty;

        /// <summary>
        /// Related domain concepts.
        /// </summary>
        public List<string> RelatedConcepts { get; set; } = [];
    }

    /// <summary>
    /// Domain processing context.
    /// </summary>
    public class DomainProcessingContext
    {
        /// <summary>
        /// Target domain.
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Industry classification.
        /// </summary>
        public string Industry { get; set; } = string.Empty;

        /// <summary>
        /// Business context.
        /// </summary>
        public string BusinessContext { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of insights.
    /// </summary>
    public enum InsightType
    {
        /// <summary>
        /// Business process insight.
        /// </summary>
        BusinessProcess,

        /// <summary>
        /// Regulatory compliance insight.
        /// </summary>
        RegulatoryCompliance
    }

    /// <summary>
    /// Impact levels.
    /// </summary>
    public enum ImpactLevel
    {
        /// <summary>
        /// Critical impact.
        /// </summary>
        Critical,

        /// <summary>
        /// High impact.
        /// </summary>
        High,

        /// <summary>
        /// Medium impact.
        /// </summary>
        Medium
    }

    /// <summary>
    /// Pattern categories.
    /// </summary>
    public enum PatternCategory
    {
        /// <summary>
        /// Security pattern.
        /// </summary>
        Security
    }

    /// <summary>
    /// Domain validation issue types.
    /// </summary>
    public enum DomainValidationIssueType
    {
        /// <summary>
        /// Inadequate business context.
        /// </summary>
        InadequateBusinessContext
    }

    /// <summary>
    /// Improvement types.
    /// </summary>
    public enum ImprovementType
    {
        /// <summary>
        /// Terminology improvement.
        /// </summary>
        Terminology
    }
}