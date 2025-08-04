using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Service for extracting, validating, and enhancing feature requirements from natural language input.
    /// This is the core service for Phase 5.1.2 of the Feature Factory pipeline.
    /// </summary>
    public interface IFeatureRequirementsExtractor
    {
        /// <summary>
        /// Extracts feature requirements from natural language input with comprehensive analysis.
        /// </summary>
        /// <param name="input">Natural language description of the feature</param>
        /// <param name="context">Processing context including domain and constraints</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Extraction result with requirements and validation</returns>
        Task<FeatureRequirementResult> ExtractRequirementsAsync(
            string input,
            ProcessingContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates extracted requirements against business rules and domain context.
        /// </summary>
        /// <param name="requirements">Requirements to validate</param>
        /// <param name="context">Validation context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with issues and recommendations</returns>
        Task<ValidationResult> ValidateRequirementsAsync(
            List<FeatureRequirement> requirements,
            ValidationContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enhances requirements with additional context, business rules, and technical specifications.
        /// </summary>
        /// <param name="requirements">Requirements to enhance</param>
        /// <param name="context">Enhancement context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Enhanced requirements with additional details</returns>
        Task<FeatureRequirementResult> EnhanceRequirementsAsync(
            List<FeatureRequirement> requirements,
            ProcessingContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Prioritizes requirements based on business value, technical complexity, and risk.
        /// </summary>
        /// <param name="requirements">Requirements to prioritize</param>
        /// <param name="context">Prioritization context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Prioritization result with scores and recommendations</returns>
        Task<PrioritizationResult> PrioritizeRequirementsAsync(
            List<FeatureRequirement> requirements,
            ProcessingContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes requirements for completeness, clarity, and consistency.
        /// </summary>
        /// <param name="requirements">Requirements to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analysis result with metrics and insights</returns>
        Task<RequirementsAnalysisResult> AnalyzeRequirementsAsync(
            List<FeatureRequirement> requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates acceptance criteria for requirements based on business rules and user stories.
        /// </summary>
        /// <param name="requirements">Requirements to generate criteria for</param>
        /// <param name="context">Generation context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated acceptance criteria</returns>
        Task<AcceptanceCriteriaResult> GenerateAcceptanceCriteriaAsync(
            List<FeatureRequirement> requirements,
            ProcessingContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of requirements analysis with metrics and insights.
    /// </summary>
    public class RequirementsAnalysisResult
    {
        /// <summary>
        /// Whether the analysis was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Overall completeness score (0.0 to 1.0).
        /// </summary>
        public double CompletenessScore { get; set; }

        /// <summary>
        /// Overall clarity score (0.0 to 1.0).
        /// </summary>
        public double ClarityScore { get; set; }

        /// <summary>
        /// Overall consistency score (0.0 to 1.0).
        /// </summary>
        public double ConsistencyScore { get; set; }

        /// <summary>
        /// Overall feasibility score (0.0 to 1.0).
        /// </summary>
        public double FeasibilityScore { get; set; }

        /// <summary>
        /// Analysis insights and observations.
        /// </summary>
        public List<RequirementInsight> Insights { get; set; } = new List<RequirementInsight>();

        /// <summary>
        /// Identified gaps and missing information.
        /// </summary>
        public List<RequirementGap> Gaps { get; set; } = new List<RequirementGap>();

        /// <summary>
        /// Recommendations for improvement.
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Insight about a specific requirement or requirement set.
    /// </summary>
    public class RequirementInsight
    {
        /// <summary>
        /// Type of insight.
        /// </summary>
        public RequirementInsightType Type { get; set; }

        /// <summary>
        /// Description of the insight.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Related requirement IDs.
        /// </summary>
        public List<string> RequirementIds { get; set; } = new List<string>();

        /// <summary>
        /// Confidence score for the insight.
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Impact level of the insight.
        /// </summary>
        public ImpactLevel Impact { get; set; }

        /// <summary>
        /// Suggested actions based on the insight.
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Gap identified in requirements analysis.
    /// </summary>
    public class RequirementGap
    {
        /// <summary>
        /// Type of gap.
        /// </summary>
        public RequirementGapType Type { get; set; }

        /// <summary>
        /// Description of the gap.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Severity of the gap.
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Related requirement IDs.
        /// </summary>
        public List<string> RequirementIds { get; set; } = new List<string>();

        /// <summary>
        /// Suggested resolution for the gap.
        /// </summary>
        public string SuggestedResolution { get; set; } = string.Empty;

        /// <summary>
        /// Estimated effort to resolve the gap.
        /// </summary>
        public int EstimatedEffort { get; set; }
    }

    /// <summary>
    /// Result of acceptance criteria generation.
    /// </summary>
    public class AcceptanceCriteriaResult
    {
        /// <summary>
        /// Whether the generation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Generated acceptance criteria for each requirement.
        /// </summary>
        public List<RequirementAcceptanceCriteria> Criteria { get; set; } = new List<RequirementAcceptanceCriteria>();

        /// <summary>
        /// Overall quality score of generated criteria.
        /// </summary>
        public double QualityScore { get; set; }

        /// <summary>
        /// Processing metadata.
        /// </summary>
        public ProcessingMetadata Metadata { get; set; } = new ProcessingMetadata();
    }

    /// <summary>
    /// Acceptance criteria for a specific requirement.
    /// </summary>
    public class RequirementAcceptanceCriteria
    {
        /// <summary>
        /// ID of the requirement.
        /// </summary>
        public string RequirementId { get; set; } = string.Empty;

        /// <summary>
        /// Generated acceptance criteria.
        /// </summary>
        public List<string> Criteria { get; set; } = new List<string>();

        /// <summary>
        /// Test scenarios derived from the criteria.
        /// </summary>
        public List<TestScenario> TestScenarios { get; set; } = new List<TestScenario>();

        /// <summary>
        /// Quality score for the generated criteria.
        /// </summary>
        public double QualityScore { get; set; }
    }

    /// <summary>
    /// Test scenario derived from acceptance criteria.
    /// </summary>
    public class TestScenario
    {
        /// <summary>
        /// ID of the test scenario.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the test scenario.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the test scenario.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Preconditions for the test.
        /// </summary>
        public List<string> Preconditions { get; set; } = new List<string>();

        /// <summary>
        /// Test steps.
        /// </summary>
        public List<string> Steps { get; set; } = new List<string>();

        /// <summary>
        /// Expected results.
        /// </summary>
        public List<string> ExpectedResults { get; set; } = new List<string>();

        /// <summary>
        /// Priority of the test scenario.
        /// </summary>
        public RequirementPriority Priority { get; set; }
    }

    /// <summary>
    /// Type of requirement insight.
    /// </summary>
    public enum RequirementInsightType
    {
        /// <summary>
        /// Business value insight.
        /// </summary>
        BusinessValue,

        /// <summary>
        /// Technical complexity insight.
        /// </summary>
        TechnicalComplexity,

        /// <summary>
        /// Risk assessment insight.
        /// </summary>
        RiskAssessment,

        /// <summary>
        /// Dependency insight.
        /// </summary>
        Dependency,

        /// <summary>
        /// Integration insight.
        /// </summary>
        Integration,

        /// <summary>
        /// Performance insight.
        /// </summary>
        Performance,

        /// <summary>
        /// Security insight.
        /// </summary>
        Security,

        /// <summary>
        /// Usability insight.
        /// </summary>
        Usability,

        /// <summary>
        /// Compliance insight.
        /// </summary>
        Compliance,

        /// <summary>
        /// Scalability insight.
        /// </summary>
        Scalability
    }

    /// <summary>
    /// Type of requirement gap.
    /// </summary>
    public enum RequirementGapType
    {
        /// <summary>
        /// Missing functional requirement.
        /// </summary>
        MissingFunctionalRequirement,

        /// <summary>
        /// Missing non-functional requirement.
        /// </summary>
        MissingNonFunctionalRequirement,

        /// <summary>
        /// Missing business rule.
        /// </summary>
        MissingBusinessRule,

        /// <summary>
        /// Missing acceptance criteria.
        /// </summary>
        MissingAcceptanceCriteria,

        /// <summary>
        /// Missing technical specification.
        /// </summary>
        MissingTechnicalSpecification,

        /// <summary>
        /// Missing integration point.
        /// </summary>
        MissingIntegrationPoint,

        /// <summary>
        /// Missing data model.
        /// </summary>
        MissingDataModel,

        /// <summary>
        /// Missing workflow.
        /// </summary>
        MissingWorkflow,

        /// <summary>
        /// Missing stakeholder consideration.
        /// </summary>
        MissingStakeholderConsideration,

        /// <summary>
        /// Missing compliance requirement.
        /// </summary>
        MissingComplianceRequirement
    }
} 