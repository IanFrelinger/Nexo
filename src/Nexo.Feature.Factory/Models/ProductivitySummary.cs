namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity summary
/// </summary>
public record ProductivitySummary
{
    public double CurrentProductivityMultiplier { get; init; }
    public int FeaturesThisWeek { get; init; }
    public int FeaturesThisMonth { get; init; }
    public double TimeSavingsThisWeek { get; init; }
    public double CostSavingsThisMonth { get; init; }
    public string Trend { get; init; } = string.Empty;
} 