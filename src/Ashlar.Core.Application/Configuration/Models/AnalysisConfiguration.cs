namespace Ashlar.Core.Application.Configuration.Models;

/// <summary>
/// Configuration for analysis operations.
/// 
/// Contains:
/// - Enabled analysis rules
/// - Rule-specific settings
/// - Complexity thresholds
/// - Feature flags for security scanning and code quality
/// 
/// Used by IAnalysisService to configure analysis behavior.
/// Part of AshlarConfiguration.
/// </summary>
public record AnalysisConfiguration
{
    /// <summary>Analysis rule identifiers enabled for this configuration.</summary>
    public IReadOnlyList<string> EnabledRules { get; init; } = Array.Empty<string>();

    /// <summary>Per-rule settings keyed by rule identifier.</summary>
    public IReadOnlyDictionary<string, object> RuleSettings { get; init; } = new Dictionary<string, object>();

    /// <summary>Maximum cyclomatic complexity threshold before flagging.</summary>
    public int MaxComplexityThreshold { get; init; } = 20;

    /// <summary>Whether security scanning rules are enabled.</summary>
    public bool EnableSecurityScan { get; init; } = true;

    /// <summary>Whether code quality rules are enabled.</summary>
    public bool EnableCodeQuality { get; init; } = true;
}
