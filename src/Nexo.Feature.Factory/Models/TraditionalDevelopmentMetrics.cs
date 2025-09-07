using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Traditional development metrics
/// </summary>
public record TraditionalDevelopmentMetrics
{
    public int FeatureCount { get; init; }
    public TimeSpan AverageDevelopmentTime { get; init; }
    public double AverageCost { get; init; }
    public double QualityScore { get; init; }
    public double SuccessRate { get; init; }
    public List<string> CommonIssues { get; init; } = new();
} 