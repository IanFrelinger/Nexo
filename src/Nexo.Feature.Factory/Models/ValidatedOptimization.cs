using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Validated optimization
/// </summary>
public record ValidatedOptimization
{
    public string SuggestionId { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public double ValidationScore { get; init; }
    public List<string> ValidationIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
    public Dictionary<string, object> ValidationData { get; init; } = new();
} 