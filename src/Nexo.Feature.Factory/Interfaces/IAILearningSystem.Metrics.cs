using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Learning metrics and model optimization models and data structures
/// </summary>
public partial interface IAILearningSystem
{
    // This partial interface contains metrics and optimization models
}

/// <summary>
/// AI learning metrics request
/// </summary>
public record AILearningMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ModelType { get; init; }
    public List<string> Metrics { get; init; } = new();
    public bool IncludeTrends { get; init; }
    public Dictionary<string, object> MetricsParameters { get; init; } = new();
}

/// <summary>
/// AI learning metrics
/// </summary>
public record AILearningMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double OverallAccuracy { get; init; }
    public double LearningRate { get; init; }
    public int PatternsLearned { get; init; }
    public int KnowledgeItemsAccumulated { get; init; }
    public double ImprovementRate { get; init; }
    public List<ModelPerformance> ModelPerformance { get; init; } = new();
    public List<LearningTrend> LearningTrends { get; init; } = new();
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Model performance
/// </summary>
public record ModelPerformance
{
    public string ModelId { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
    public double Accuracy { get; init; }
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1Score { get; init; }
    public int TrainingExamples { get; init; }
    public DateTime LastUpdated { get; init; }
    public Dictionary<string, double> PerformanceData { get; init; } = new();
}

/// <summary>
/// Learning trend
/// </summary>
public record LearningTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
}

/// <summary>
/// Model optimization request
/// </summary>
public record ModelOptimizationRequest
{
    public List<string> ModelIds { get; init; } = new();
    public string OptimizationType { get; init; } = string.Empty;
    public bool EnableHyperparameterTuning { get; init; }
    public bool EnableArchitectureOptimization { get; init; }
    public Dictionary<string, object> OptimizationParameters { get; init; } = new();
}

/// <summary>
/// Model optimization result
/// </summary>
public record ModelOptimizationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<OptimizedModel> OptimizedModels { get; init; } = new();
    public int ModelsOptimized { get; init; }
    public double AverageImprovement { get; init; }
    public TimeSpan OptimizationDuration { get; init; }
    public Dictionary<string, double> OptimizationMetrics { get; init; } = new();
    public DateTime OptimizedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Optimized model
/// </summary>
public record OptimizedModel
{
    public string ModelId { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
    public double BeforeAccuracy { get; init; }
    public double AfterAccuracy { get; init; }
    public double Improvement { get; init; }
    public List<string> OptimizationsApplied { get; init; } = new();
    public DateTime OptimizedAt { get; init; }
    public Dictionary<string, object> OptimizationData { get; init; } = new();
}
