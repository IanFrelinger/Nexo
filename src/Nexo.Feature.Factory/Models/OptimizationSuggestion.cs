using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization suggestion
/// </summary>
public record OptimizationSuggestion
{
    public string SuggestionId { get; init; } = string.Empty;
    public string SuggestionType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public double Priority { get; init; }
    public double ExpectedImpact { get; init; }
    public string Effort { get; init; } = string.Empty;
    public List<string> AffectedComponents { get; init; } = new();
    public List<string> ImplementationSteps { get; init; } = new();
    public Dictionary<string, object> SuggestionData { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
} 