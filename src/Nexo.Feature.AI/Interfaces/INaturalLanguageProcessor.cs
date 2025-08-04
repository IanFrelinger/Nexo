using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Processes natural language feature requirements and converts them into structured data.
    /// </summary>
    public interface INaturalLanguageProcessor
    {
        /// <summary>
        /// Processes natural language input and extracts feature requirements.
        /// </summary>
        /// <param name="input">Natural language input describing the feature requirements.</param>
        /// <param name="context">Processing context including domain, business rules, and constraints.</param>
        /// <returns>Processed feature requirements with structured data.</returns>
        Task<FeatureRequirementResult> ProcessRequirementsAsync(string input, ProcessingContext context);

        /// <summary>
        /// Validates natural language input for completeness and clarity.
        /// </summary>
        /// <param name="input">Natural language input to validate.</param>
        /// <param name="context">Validation context including required fields and business rules.</param>
        /// <returns>Validation result with issues and recommendations.</returns>
        Task<ValidationResult> ValidateInputAsync(string input, ValidationContext context);

        /// <summary>
        /// Extracts specific components from natural language input.
        /// </summary>
        /// <param name="input">Natural language input.</param>
        /// <param name="extractionType">Type of extraction to perform.</param>
        /// <param name="context">Extraction context.</param>
        /// <returns>Extracted components.</returns>
        Task<ExtractionResult> ExtractComponentsAsync(string input, ExtractionType extractionType, ExtractionContext context);

        /// <summary>
        /// Recognizes and processes domain-specific terminology.
        /// </summary>
        /// <param name="input">Input containing domain terminology.</param>
        /// <param name="domain">Target domain for terminology recognition.</param>
        /// <returns>Processed input with recognized terminology.</returns>
        Task<DomainTerminologyResult> ProcessDomainTerminologyAsync(string input, string domain);

        /// <summary>
        /// Categorizes and prioritizes feature requirements.
        /// </summary>
        /// <param name="requirements">List of feature requirements to categorize.</param>
        /// <param name="prioritizationContext">Context for prioritization including business value and constraints.</param>
        /// <returns>Categorized and prioritized requirements.</returns>
        Task<PrioritizationResult> CategorizeAndPrioritizeAsync(IEnumerable<FeatureRequirement> requirements, PrioritizationContext prioritizationContext);

        /// <summary>
        /// Checks if the processor supports the specified input format.
        /// </summary>
        /// <param name="inputFormat">Format to check support for.</param>
        /// <returns>True if the format is supported, false otherwise.</returns>
        bool SupportsFormat(InputFormat inputFormat);

        /// <summary>
        /// Gets the list of supported input formats.
        /// </summary>
        /// <returns>Collection of supported input formats.</returns>
        IEnumerable<InputFormat> GetSupportedFormats();

        /// <summary>
        /// Gets the list of supported domains for terminology processing.
        /// </summary>
        /// <returns>Collection of supported domains.</returns>
        IEnumerable<string> GetSupportedDomains();
    }

    /// <summary>
    /// Context for processing natural language requirements.
    /// </summary>
    public class ProcessingContext
    {
        /// <summary>
        /// Target domain for the feature.
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Business rules and constraints to apply.
        /// </summary>
        public IEnumerable<string> BusinessRules { get; set; } = new List<string>();

        /// <summary>
        /// Technical constraints and limitations.
        /// </summary>
        public IEnumerable<string> TechnicalConstraints { get; set; } = new List<string>();

        /// <summary>
        /// User roles and permissions context.
        /// </summary>
        public IEnumerable<string> UserRoles { get; set; } = new List<string>();

        /// <summary>
        /// Integration points and dependencies.
        /// </summary>
        public IEnumerable<string> IntegrationPoints { get; set; } = new List<string>();

        /// <summary>
        /// Quality attributes and non-functional requirements.
        /// </summary>
        public QualityAttributes QualityAttributes { get; set; } = new QualityAttributes();
    }

    /// <summary>
    /// Context for validation operations.
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// Required fields that must be present in the input.
        /// </summary>
        public IEnumerable<string> RequiredFields { get; set; } = new List<string>();

        /// <summary>
        /// Business rules to validate against.
        /// </summary>
        public IEnumerable<string> BusinessRules { get; set; } = new List<string>();

        /// <summary>
        /// Minimum completeness threshold (0.0 to 1.0).
        /// </summary>
        public double MinimumCompleteness { get; set; } = 0.8;

        /// <summary>
        /// Maximum ambiguity threshold (0.0 to 1.0).
        /// </summary>
        public double MaximumAmbiguity { get; set; } = 0.3;
    }

    /// <summary>
    /// Context for extraction operations.
    /// </summary>
    public class ExtractionContext
    {
        /// <summary>
        /// Target domain for extraction.
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Specific patterns to look for during extraction.
        /// </summary>
        public IEnumerable<string> Patterns { get; set; } = new List<string>();

        /// <summary>
        /// Whether to include confidence scores in the result.
        /// </summary>
        public bool IncludeConfidenceScores { get; set; } = true;
    }

    /// <summary>
    /// Context for prioritization operations.
    /// </summary>
    public class PrioritizationContext
    {
        /// <summary>
        /// Business value weights for different requirement types.
        /// </summary>
        public Dictionary<string, double> BusinessValueWeights { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Technical complexity weights.
        /// </summary>
        public Dictionary<string, double> TechnicalComplexityWeights { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Resource constraints and availability.
        /// </summary>
        public ResourceConstraints ResourceConstraints { get; set; } = new ResourceConstraints();

        /// <summary>
        /// Timeline constraints and deadlines.
        /// </summary>
        public TimelineConstraints TimelineConstraints { get; set; } = new TimelineConstraints();
    }

    /// <summary>
    /// Quality attributes for non-functional requirements.
    /// </summary>
    public class QualityAttributes
    {
        /// <summary>
        /// Performance requirements.
        /// </summary>
        public string Performance { get; set; } = string.Empty;

        /// <summary>
        /// Security requirements.
        /// </summary>
        public string Security { get; set; } = string.Empty;

        /// <summary>
        /// Scalability requirements.
        /// </summary>
        public string Scalability { get; set; } = string.Empty;

        /// <summary>
        /// Usability requirements.
        /// </summary>
        public string Usability { get; set; } = string.Empty;

        /// <summary>
        /// Maintainability requirements.
        /// </summary>
        public string Maintainability { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resource constraints for prioritization.
    /// </summary>
    public class ResourceConstraints
    {
        /// <summary>
        /// Available development team size.
        /// </summary>
        public int TeamSize { get; set; }

        /// <summary>
        /// Available budget.
        /// </summary>
        public decimal Budget { get; set; }

        /// <summary>
        /// Available infrastructure resources.
        /// </summary>
        public string Infrastructure { get; set; } = string.Empty;
    }

    /// <summary>
    /// Timeline constraints for prioritization.
    /// </summary>
    public class TimelineConstraints
    {
        /// <summary>
        /// Project deadline.
        /// </summary>
        public DateTime Deadline { get; set; }

        /// <summary>
        /// Sprint duration in days.
        /// </summary>
        public int SprintDuration { get; set; } = 14;

        /// <summary>
        /// Number of available sprints.
        /// </summary>
        public int AvailableSprints { get; set; }
    }

    /// <summary>
    /// Types of extraction operations.
    /// </summary>
    public enum ExtractionType
    {
        /// <summary>
        /// Extract user stories and acceptance criteria.
        /// </summary>
        UserStories,

        /// <summary>
        /// Extract business rules and logic.
        /// </summary>
        BusinessRules,

        /// <summary>
        /// Extract technical requirements.
        /// </summary>
        TechnicalRequirements,

        /// <summary>
        /// Extract integration points.
        /// </summary>
        IntegrationPoints,

        /// <summary>
        /// Extract data models and entities.
        /// </summary>
        DataModels,

        /// <summary>
        /// Extract workflow and process flows.
        /// </summary>
        Workflows,

        /// <summary>
        /// Extract security requirements.
        /// </summary>
        SecurityRequirements,

        /// <summary>
        /// Extract performance requirements.
        /// </summary>
        PerformanceRequirements
    }

    /// <summary>
    /// Supported input formats for natural language processing.
    /// </summary>
    public enum InputFormat
    {
        /// <summary>
        /// Plain text input.
        /// </summary>
        PlainText,

        /// <summary>
        /// Markdown formatted text.
        /// </summary>
        Markdown,

        /// <summary>
        /// Product manager specification format.
        /// </summary>
        ProductSpecification,

        /// <summary>
        /// User story format.
        /// </summary>
        UserStory,

        /// <summary>
        /// Business requirement document format.
        /// </summary>
        BusinessRequirementDocument,

        /// <summary>
        /// Technical specification format.
        /// </summary>
        TechnicalSpecification,

        /// <summary>
        /// Email or communication format.
        /// </summary>
        Email,

        /// <summary>
        /// Meeting notes format.
        /// </summary>
        MeetingNotes
    }
}