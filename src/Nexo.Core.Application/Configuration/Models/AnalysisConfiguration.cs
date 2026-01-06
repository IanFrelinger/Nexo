namespace Nexo.Core.Application.Configuration.Models;

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
/// Part of NexoConfiguration.
/// </summary>
public record AnalysisConfiguration
{
    public IReadOnlyList<string> EnabledRules { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object> RuleSettings { get; init; } = new Dictionary<string, object>();
    public int MaxComplexityThreshold { get; init; } = 20;
    public bool EnableSecurityScan { get; init; } = true;
    public bool EnableCodeQuality { get; init; } = true;
}

