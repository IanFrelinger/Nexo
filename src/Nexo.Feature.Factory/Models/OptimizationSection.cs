using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization section
/// </summary>
public record OptimizationSection
{
    public string SectionName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SuggestionCount { get; init; }
    public int ImplementedCount { get; init; }
    public int PendingCount { get; init; }
    public double ImprovementRate { get; init; }
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
} 