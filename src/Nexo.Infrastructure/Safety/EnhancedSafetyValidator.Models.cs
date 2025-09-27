using System;
using System.Collections.Generic;

namespace Nexo.Infrastructure.Safety
{
    /// <summary>
    /// Data models for EnhancedSafetyValidator.
    /// </summary>
    public partial class EnhancedSafetyValidator
    {
        // This partial class contains the data models used by EnhancedSafetyValidator
    }

    /// <summary>
    /// Represents a safety validation result
    /// </summary>
    public class SafetyValidationResult
    {
        public bool IsValid { get; set; }
        public bool RequiresHumanReview { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<SafetyIssue> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Represents a safety issue found in code
    /// </summary>
    public class SafetyIssue
    {
        public SafetyIssueType Type { get; set; }
        public SafetySeverity Severity { get; set; }
        public string Message { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = "";
    }

    /// <summary>
    /// Types of safety issues
    /// </summary>
    public enum SafetyIssueType
    {
        FileSystemAccess,
        NetworkAccess,
        SystemCommand,
        DataDestruction,
        Security,
        ResourceExhaustion
    }

    /// <summary>
    /// Severity levels for safety issues
    /// </summary>
    public enum SafetySeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}
