namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Comparison detail
/// </summary>
public record ComparisonDetail
{
    public string MetricName { get; init; } = string.Empty;
    public double TraditionalValue { get; init; }
    public double FeatureFactoryValue { get; init; }
    public double Improvement { get; init; }
    public string Unit { get; init; } = string.Empty;
} 