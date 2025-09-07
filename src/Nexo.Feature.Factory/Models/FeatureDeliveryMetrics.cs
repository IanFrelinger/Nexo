using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Feature delivery metrics
/// </summary>
public record FeatureDeliveryMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int FeaturesDelivered { get; init; }
    public int FeaturesPerDay { get; init; }
    public int FeaturesPerWeek { get; init; }
    public int FeaturesPerMonth { get; init; }
    public TimeSpan AverageDeliveryTime { get; init; }
    public double OnTimeDeliveryRate { get; init; }
    public double QualityScore { get; init; }
    public Dictionary<string, int> FeaturesByType { get; init; } = new();
    public Dictionary<string, int> FeaturesByPriority { get; init; } = new();
    public List<DeliveryTrend> Trends { get; init; } = new();
} 