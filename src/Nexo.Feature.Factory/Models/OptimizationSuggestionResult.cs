using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization suggestion result
/// </summary>
public record OptimizationSuggestionResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<OptimizationSuggestion> Suggestions { get; init; } = new();
    public int SuggestionsGenerated { get; init; }
    public int HighPrioritySuggestions { get; init; }
    public double SuggestionQuality { get; init; }
    public TimeSpan GenerationDuration { get; init; }
    public Dictionary<string, double> GenerationMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 