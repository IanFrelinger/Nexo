using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Development comparison metrics
/// </summary>
public record DevelopmentComparisonMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public TraditionalDevelopmentMetrics Traditional { get; init; } = new();
    public FeatureFactoryMetrics FeatureFactory { get; init; } = new();
    public ComparisonSummary Summary { get; init; } = new();
    public List<ComparisonDetail> Details { get; init; } = new();
} 