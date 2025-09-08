using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Represents the result of validating code against coding standards.
    /// </summary>
    public class CodingStandardValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the overall score (0-100) for the code quality.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Gets or sets the list of violations found during validation.
        /// </summary>
        public List<CodingStandardViolation> Violations { get; set; } = new List<CodingStandardViolation>();

        /// <summary>
        /// Gets or sets the list of suggestions for improvement.
        /// </summary>
        public List<CodingStandardSuggestion> Suggestions { get; set; } = new List<CodingStandardSuggestion>();

        /// <summary>
        /// Gets or sets the summary of the validation results.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the validation was performed.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the coding standards that were applied during validation.
        /// </summary>
        public List<string> AppliedStandards { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets additional metadata about the validation.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the count of violations by severity.
        /// </summary>
        public Dictionary<CodingStandardSeverity, int> ViolationCounts
        {
            get
            {
                var counts = new Dictionary<CodingStandardSeverity, int>();
                foreach (CodingStandardSeverity severity in Enum.GetValues<CodingStandardSeverity>())
                {
                    counts[severity] = 0;
                }

                foreach (var violation in Violations)
                {
                    counts[violation.Severity]++;
                }

                return counts;
            }
        }
    }

    /// <summary>
    /// Represents a violation of a coding standard rule.
    /// </summary>
    public class CodingStandardViolation
    {
        /// <summary>
        /// Gets or sets the unique identifier of the violation.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the rule that was violated.
        /// </summary>
        public string RuleId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the rule that was violated.
        /// </summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity of the violation.
        /// </summary>
        public CodingStandardSeverity Severity { get; set; } = CodingStandardSeverity.Warning;

        /// <summary>
        /// Gets or sets the error message describing the violation.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path where the violation occurred.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the line number where the violation occurred.
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the column number where the violation occurred.
        /// </summary>
        public int? ColumnNumber { get; set; }

        /// <summary>
        /// Gets or sets the code snippet that violated the rule.
        /// </summary>
        public string? CodeSnippet { get; set; }

        /// <summary>
        /// Gets or sets the suggested fix for the violation.
        /// </summary>
        public string? SuggestedFix { get; set; }

        /// <summary>
        /// Gets or sets the category of the violation.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional context about the violation.
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a suggestion for improving code quality.
    /// </summary>
    public class CodingStandardSuggestion
    {
        /// <summary>
        /// Gets or sets the unique identifier of the suggestion.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the title of the suggestion.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the suggestion.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category of the suggestion.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the priority of the suggestion.
        /// </summary>
        public CodingStandardSuggestionPriority Priority { get; set; } = CodingStandardSuggestionPriority.Medium;

        /// <summary>
        /// Gets or sets the file path where the suggestion applies.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the line number where the suggestion applies.
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the suggested code improvement.
        /// </summary>
        public string? SuggestedCode { get; set; }

        /// <summary>
        /// Gets or sets additional context about the suggestion.
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the priority level of a coding standard suggestion.
    /// </summary>
    public enum CodingStandardSuggestionPriority
    {
        /// <summary>
        /// Low priority suggestion.
        /// </summary>
        Low,

        /// <summary>
        /// Medium priority suggestion.
        /// </summary>
        Medium,

        /// <summary>
        /// High priority suggestion.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority suggestion.
        /// </summary>
        Critical
    }
}
