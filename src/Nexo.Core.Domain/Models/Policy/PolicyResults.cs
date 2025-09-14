using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Policy
{
    /// <summary>
    /// Result of policy validation
    /// </summary>
    public class PolicyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of safety policy application
    /// </summary>
    public class SafetyPolicyResult
    {
        public bool Passed { get; set; }
        public List<SafetyViolation> Violations { get; set; } = new List<SafetyViolation>();
        public double SafetyScore { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of quality policy application
    /// </summary>
    public class QualityPolicyResult
    {
        public bool Passed { get; set; }
        public List<QualityViolation> Violations { get; set; } = new List<QualityViolation>();
        public double QualityScore { get; set; }
        public Dictionary<string, double> GateScores { get; set; } = new Dictionary<string, double>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of policy execution
    /// </summary>
    public class PolicyExecutionResult
    {
        public bool Passed { get; set; }
        public SafetyPolicyResult? SafetyResult { get; set; }
        public QualityPolicyResult? QualityResult { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string ReportPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Safety violation
    /// </summary>
    public class SafetyViolation
    {
        public string RuleId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
        public int? ColumnNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FixSuggestion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Quality violation
    /// </summary>
    public class QualityViolation
    {
        public string RuleId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Gate { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
        public int? ColumnNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FixSuggestion { get; set; } = string.Empty;
        public double ImpactScore { get; set; }
    }
}
