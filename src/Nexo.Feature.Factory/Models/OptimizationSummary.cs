namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization summary
/// </summary>
public record OptimizationSummary
{
    public int TotalSuggestions { get; init; }
    public int ImplementedSuggestions { get; init; }
    public int PendingSuggestions { get; init; }
    public double OverallImprovement { get; init; }
    public double OptimizationEfficiency { get; init; }
    public string Status { get; init; } = string.Empty;
} 