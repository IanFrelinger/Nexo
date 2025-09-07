using System;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Delivery trend
/// </summary>
public record DeliveryTrend
{
    public DateTime Date { get; init; }
    public int FeaturesDelivered { get; init; }
    public TimeSpan AverageTime { get; init; }
    public double QualityScore { get; init; }
} 