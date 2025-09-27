using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Usage pattern analysis models and data structures
/// </summary>
public partial interface IAILearningSystem
{
    // This partial interface contains usage analysis models
}

/// <summary>
/// Usage pattern analysis request
/// </summary>
public record UsagePatternAnalysisRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Domain { get; init; }
    public string? Industry { get; init; }
    public List<string> AnalysisTypes { get; init; } = new();
    public bool EnablePredictiveAnalysis { get; init; }
    public Dictionary<string, object> AnalysisParameters { get; init; } = new();
}

/// <summary>
/// Usage pattern analysis result
/// </summary>
public record UsagePatternAnalysisResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<UsagePattern> DiscoveredPatterns { get; init; } = new();
    public List<UsageTrend> UsageTrends { get; init; } = new();
    public List<UsageInsight> Insights { get; init; } = new();
    public int PatternsAnalyzed { get; init; }
    public int NewPatternsFound { get; init; }
    public double AnalysisAccuracy { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
    public Dictionary<string, double> AnalysisMetrics { get; init; } = new();
    public DateTime AnalyzedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Usage pattern
/// </summary>
public record UsagePattern
{
    public string PatternId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Frequency { get; init; }
    public double Confidence { get; init; }
    public List<string> Users { get; init; } = new();
    public List<string> Features { get; init; } = new();
    public DateTime FirstSeen { get; init; }
    public DateTime LastSeen { get; init; }
    public Dictionary<string, object> PatternData { get; init; } = new();
}

/// <summary>
/// Usage trend
/// </summary>
public record UsageTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string TrendType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public string Direction { get; init; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
}

/// <summary>
/// Trend data point
/// </summary>
public record TrendDataPoint
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Usage insight
/// </summary>
public record UsageInsight
{
    public string InsightId { get; init; } = string.Empty;
    public string InsightType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Impact { get; init; } = string.Empty;
    public List<string> Recommendations { get; init; } = new();
    public DateTime DiscoveredAt { get; init; }
    public Dictionary<string, object> InsightData { get; init; } = new();
}
