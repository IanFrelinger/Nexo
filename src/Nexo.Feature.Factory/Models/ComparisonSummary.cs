namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Comparison summary
/// </summary>
public record ComparisonSummary
{
    public double ProductivityImprovement { get; init; }
    public double TimeSavings { get; init; }
    public double CostSavings { get; init; }
    public double QualityImprovement { get; init; }
    public string Recommendation { get; init; } = string.Empty;
} 