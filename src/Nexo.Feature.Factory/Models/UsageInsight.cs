using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Represents an insight derived from usage analysis.
/// </summary>
public record UsageInsight
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string InsightType { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public double Impact { get; init; }
    public double Confidence { get; init; }
    public List<string> Recommendations { get; init; } = new();
    public List<string> RelatedPatterns { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public DateTime ValidUntil { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
