using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Pattern learning models and data structures
/// </summary>
public partial interface IAILearningSystem
{
    // This partial interface contains pattern learning models
}

/// <summary>
/// Feature pattern learning request
/// </summary>
public record FeaturePatternLearningRequest
{
    public List<FeaturePattern> Patterns { get; init; } = new();
    public string Domain { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public LearningMode Mode { get; init; }
    public bool EnableRealTimeLearning { get; init; }
    public Dictionary<string, object> LearningParameters { get; init; } = new();
}

/// <summary>
/// Learning modes
/// </summary>
public enum LearningMode
{
    Supervised,
    Unsupervised,
    Reinforcement,
    Transfer,
    Active
}

/// <summary>
/// Feature pattern
/// </summary>
public record FeaturePattern
{
    public string PatternId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = new();
    public List<string> Components { get; init; } = new();
    public Dictionary<string, object> Attributes { get; init; } = new();
    public double Confidence { get; init; }
    public DateTime DiscoveredAt { get; init; }
    public int UsageCount { get; init; }
}

/// <summary>
/// Feature pattern learning result
/// </summary>
public record FeaturePatternLearningResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<LearnedPattern> LearnedPatterns { get; init; } = new();
    public int PatternsProcessed { get; init; }
    public int NewPatternsDiscovered { get; init; }
    public int PatternsImproved { get; init; }
    public double LearningAccuracy { get; init; }
    public TimeSpan LearningDuration { get; init; }
    public Dictionary<string, double> PerformanceMetrics { get; init; } = new();
    public DateTime LearnedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Learned pattern
/// </summary>
public record LearnedPattern
{
    public string PatternId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public double Accuracy { get; init; }
    public int TrainingExamples { get; init; }
    public List<string> Applications { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime LearnedAt { get; init; }
}
