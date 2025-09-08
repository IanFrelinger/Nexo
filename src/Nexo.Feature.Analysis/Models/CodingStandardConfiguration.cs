using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Represents the configuration for coding standards enforcement.
    /// </summary>
    public class CodingStandardConfiguration
    {
        /// <summary>
        /// Gets or sets the unique identifier of the configuration.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the configuration.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the configuration.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version of the configuration.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets whether the configuration is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the coding standards to apply.
        /// </summary>
        public List<CodingStandard> Standards { get; set; } = new List<CodingStandard>();

        /// <summary>
        /// Gets or sets the global settings for the configuration.
        /// </summary>
        public CodingStandardGlobalSettings GlobalSettings { get; set; } = new CodingStandardGlobalSettings();

        /// <summary>
        /// Gets or sets the agent-specific settings.
        /// </summary>
        public Dictionary<string, CodingStandardAgentSettings> AgentSettings { get; set; } = new Dictionary<string, CodingStandardAgentSettings>();

        /// <summary>
        /// Gets or sets the file type specific settings.
        /// </summary>
        public Dictionary<string, CodingStandardFileTypeSettings> FileTypeSettings { get; set; } = new Dictionary<string, CodingStandardFileTypeSettings>();

        /// <summary>
        /// Gets or sets additional metadata for the configuration.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents global settings for coding standards enforcement.
    /// </summary>
    public class CodingStandardGlobalSettings
    {
        /// <summary>
        /// Gets or sets whether to fail on critical violations.
        /// </summary>
        public bool FailOnCriticalViolations { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to fail on error violations.
        /// </summary>
        public bool FailOnErrorViolations { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of violations allowed before failing.
        /// </summary>
        public int MaxViolationsAllowed { get; set; } = 10;

        /// <summary>
        /// Gets or sets the minimum quality score required.
        /// </summary>
        public int MinimumQualityScore { get; set; } = 80;

        /// <summary>
        /// Gets or sets whether to auto-fix violations when possible.
        /// </summary>
        public bool AutoFixEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout for validation in milliseconds.
        /// </summary>
        public int ValidationTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets whether to include suggestions in the results.
        /// </summary>
        public bool IncludeSuggestions { get; set; } = true;

        /// <summary>
        /// Gets or sets the verbosity level for validation results.
        /// </summary>
        public CodingStandardVerbosityLevel VerbosityLevel { get; set; } = CodingStandardVerbosityLevel.Normal;

        /// <summary>
        /// Gets or sets the file patterns to include in validation.
        /// </summary>
        public List<string> IncludePatterns { get; set; } = new List<string> { "*.cs", "*.js", "*.ts", "*.py", "*.java" };

        /// <summary>
        /// Gets or sets the file patterns to exclude from validation.
        /// </summary>
        public List<string> ExcludePatterns { get; set; } = new List<string> { "*.generated.cs", "*.designer.cs", "bin/**", "obj/**" };
    }

    /// <summary>
    /// Represents agent-specific settings for coding standards enforcement.
    /// </summary>
    public class CodingStandardAgentSettings
    {
        /// <summary>
        /// Gets or sets the agent identifier.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether coding standards are enabled for this agent.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the specific standards to apply to this agent.
        /// </summary>
        public List<string> AppliedStandards { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the specific rules to exclude for this agent.
        /// </summary>
        public List<string> ExcludedRules { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the severity threshold for this agent.
        /// </summary>
        public CodingStandardSeverity SeverityThreshold { get; set; } = CodingStandardSeverity.Warning;

        /// <summary>
        /// Gets or sets whether to auto-fix violations for this agent.
        /// </summary>
        public bool AutoFixEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the custom settings for this agent.
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents file type specific settings for coding standards enforcement.
    /// </summary>
    public class CodingStandardFileTypeSettings
    {
        /// <summary>
        /// Gets or sets the file extension or pattern.
        /// </summary>
        public string FilePattern { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether standards are enabled for this file type.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the specific standards to apply to this file type.
        /// </summary>
        public List<string> AppliedStandards { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the specific rules to exclude for this file type.
        /// </summary>
        public List<string> ExcludedRules { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the severity threshold for this file type.
        /// </summary>
        public CodingStandardSeverity SeverityThreshold { get; set; } = CodingStandardSeverity.Warning;

        /// <summary>
        /// Gets or sets the custom settings for this file type.
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the verbosity level for coding standards validation.
    /// </summary>
    public enum CodingStandardVerbosityLevel
    {
        /// <summary>
        /// Minimal output - only critical violations.
        /// </summary>
        Minimal,

        /// <summary>
        /// Normal output - errors and warnings.
        /// </summary>
        Normal,

        /// <summary>
        /// Detailed output - all violations and suggestions.
        /// </summary>
        Detailed,

        /// <summary>
        /// Verbose output - comprehensive analysis with context.
        /// </summary>
        Verbose
    }
}
