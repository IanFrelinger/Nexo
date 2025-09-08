using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Represents a coding standard that can be applied to code generation.
    /// </summary>
    public class CodingStandard
    {
        /// <summary>
        /// Gets or sets the unique identifier of the coding standard.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the coding standard.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the coding standard.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version of the coding standard.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the programming language this standard applies to.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the framework or platform this standard applies to.
        /// </summary>
        public string? Framework { get; set; }

        /// <summary>
        /// Gets or sets the rules that define this coding standard.
        /// </summary>
        public List<CodingStandardRule> Rules { get; set; } = new List<CodingStandardRule>();

        /// <summary>
        /// Gets or sets whether this standard is enabled by default.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the priority of this standard (higher values take precedence).
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets additional metadata for this coding standard.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a rule within a coding standard.
    /// </summary>
    public class CodingStandardRule
    {
        /// <summary>
        /// Gets or sets the unique identifier of the rule.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the rule.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the rule.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category of the rule (e.g., "Naming", "Formatting", "Architecture").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity level of the rule.
        /// </summary>
        public CodingStandardSeverity Severity { get; set; } = CodingStandardSeverity.Warning;

        /// <summary>
        /// Gets or sets the type of validation this rule performs.
        /// </summary>
        public CodingStandardRuleType Type { get; set; } = CodingStandardRuleType.Pattern;

        /// <summary>
        /// Gets or sets the validation pattern or expression.
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message to display when the rule is violated.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the suggested fix for the rule violation.
        /// </summary>
        public string? SuggestedFix { get; set; }

        /// <summary>
        /// Gets or sets whether this rule is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the file patterns this rule applies to (e.g., "*.cs", "*.js").
        /// </summary>
        public List<string> FilePatterns { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets additional parameters for the rule.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the severity level of a coding standard rule.
    /// </summary>
    public enum CodingStandardSeverity
    {
        /// <summary>
        /// Information level - provides suggestions.
        /// </summary>
        Info,

        /// <summary>
        /// Warning level - indicates potential issues.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level - indicates violations that should be fixed.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level - indicates severe violations that must be fixed.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Represents the type of validation a coding standard rule performs.
    /// </summary>
    public enum CodingStandardRuleType
    {
        /// <summary>
        /// Regular expression pattern matching.
        /// </summary>
        Pattern,

        /// <summary>
        /// AST (Abstract Syntax Tree) analysis.
        /// </summary>
        Ast,

        /// <summary>
        /// Custom validation function.
        /// </summary>
        Custom,

        /// <summary>
        /// File structure validation.
        /// </summary>
        Structure,

        /// <summary>
        /// Naming convention validation.
        /// </summary>
        Naming,

        /// <summary>
        /// Code formatting validation.
        /// </summary>
        Formatting,

        /// <summary>
        /// Architecture pattern validation.
        /// </summary>
        Architecture,

        /// <summary>
        /// Security validation.
        /// </summary>
        Security,

        /// <summary>
        /// Performance validation.
        /// </summary>
        Performance
    }
}
