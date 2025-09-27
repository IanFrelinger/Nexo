using System;

namespace Nexo.Core.Domain.Models.Safety
{
    /// <summary>
    /// Represents a safety issue found during code validation
    /// </summary>
    public partial class SafetyIssue
    {
        /// <summary>
        /// Type of safety issue
        /// </summary>
        public SafetyIssueType Type { get; set; }

        /// <summary>
        /// Severity level of the issue
        /// </summary>
        public SafetySeverity Severity { get; set; }

        /// <summary>
        /// Human-readable message describing the issue
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Suggested fix for the issue
        /// </summary>
        public string FixSuggestion { get; set; } = string.Empty;

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
    /// Types of safety issues that can be detected
    /// </summary>
    public enum SafetyIssueType
    {
        /// <summary>
        /// No issue
        /// </summary>
        None,

        /// <summary>
        /// Malicious code patterns
        /// </summary>
        MaliciousCode,

        /// <summary>
        /// Security vulnerabilities
        /// </summary>
        SecurityVulnerability,

        /// <summary>
        /// Code quality issues
        /// </summary>
        CodeQuality,

        /// <summary>
        /// Performance issues
        /// </summary>
        Performance,

        /// <summary>
        /// Resource usage issues
        /// </summary>
        ResourceUsage
    }

    /// <summary>
    /// Severity levels for safety issues
    /// </summary>
    public enum SafetySeverity
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
