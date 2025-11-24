namespace Nexo.Core.Application.Configuration.Models;

/// <summary>
/// Configuration for analysis operations.
/// </summary>
public record AnalysisConfiguration
{
    public IReadOnlyList<string> EnabledRules { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object> RuleSettings { get; init; } = new Dictionary<string, object>();
    public int MaxComplexityThreshold { get; init; } = 20;
    public bool EnableSecurityScan { get; init; } = true;
    public bool EnableCodeQuality { get; init; } = true;
}

