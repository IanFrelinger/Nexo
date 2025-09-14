using System;

namespace Nexo.Core.Domain.Models.CodeQuality
{
    /// <summary>
    /// Represents a quality issue found during code analysis
    /// </summary>
    public class QualityIssue
    {
        /// <summary>
        /// Type of quality issue
        /// </summary>
        public IssueType Type { get; set; }

        /// <summary>
        /// Severity level of the issue
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Title of the issue
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Suggested fix for the issue
        /// </summary>
        public string FixSuggestion { get; set; } = string.Empty;

        /// <summary>
        /// Impact score (0-10)
        /// </summary>
        public double ImpactScore { get; set; }

        /// <summary>
        /// Line number where the issue was found (if applicable)
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Column number where the issue was found (if applicable)
        /// </summary>
        public int? ColumnNumber { get; set; }
    }

    /// <summary>
    /// Types of quality issues that can be detected
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// No issue
        /// </summary>
        None,

        /// <summary>
        /// Code quality issues
        /// </summary>
        CodeQuality,

        /// <summary>
        /// Security issues
        /// </summary>
        Security,

        /// <summary>
        /// Performance issues
        /// </summary>
        Performance,

        /// <summary>
        /// Maintainability issues
        /// </summary>
        Maintainability,

        /// <summary>
        /// Testability issues
        /// </summary>
        Testability,

        /// <summary>
        /// Documentation issues
        /// </summary>
        Documentation,

        /// <summary>
        /// Readability issues
        /// </summary>
        Readability
    }

    /// <summary>
    /// Severity levels for quality issues
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// No severity
        /// </summary>
        None,

        /// <summary>
        /// Low severity
        /// </summary>
        Low,

        /// <summary>
        /// Medium severity
        /// </summary>
        Medium,

        /// <summary>
        /// High severity
        /// </summary>
        High,

        /// <summary>
        /// Critical severity
        /// </summary>
        Critical
    }
}
