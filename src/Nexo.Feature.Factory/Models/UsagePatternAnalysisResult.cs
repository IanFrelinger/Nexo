using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Usage pattern analysis result
/// </summary>
public record UsagePatternAnalysisResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<UsagePattern> DiscoveredPatterns { get; init; } = new();
    public List<UsageAnomaly> DetectedAnomalies { get; init; } = new();
    public List<UsageTrend> UsageTrends { get; init; } = new();
    public List<UsageInsight> Insights { get; init; } = new();
    public int PatternsAnalyzed { get; init; }
    public int AnomaliesDetected { get; init; }
    public double AnalysisAccuracy { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
    public Dictionary<string, double> AnalysisMetrics { get; init; } = new();
    public DateTime AnalyzedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 