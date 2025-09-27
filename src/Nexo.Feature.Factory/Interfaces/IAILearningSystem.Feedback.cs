using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Learning feedback models and data structures
/// </summary>
public partial interface IAILearningSystem
{
    // This partial interface contains feedback models
}

/// <summary>
/// Learning feedback request
/// </summary>
public record LearningFeedbackRequest
{
    public List<LearningFeedback> FeedbackItems { get; init; } = new();
    public string FeedbackType { get; init; } = string.Empty;
    public bool EnableImmediateLearning { get; init; }
    public bool EnableBatchLearning { get; init; }
    public Dictionary<string, object> FeedbackParameters { get; init; } = new();
}

/// <summary>
/// Learning feedback
/// </summary>
public record LearningFeedback
{
    public string FeedbackId { get; init; } = string.Empty;
    public string FeatureId { get; init; } = string.Empty;
    public string FeedbackType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Rating { get; init; }
    public List<string> Tags { get; init; } = new();
    public DateTime ProvidedAt { get; init; }
    public string UserId { get; init; } = string.Empty;
    public Dictionary<string, object> FeedbackData { get; init; } = new();
}

/// <summary>
/// Learning feedback result
/// </summary>
public record LearningFeedbackResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<ProcessedFeedback> ProcessedFeedback { get; init; } = new();
    public int FeedbackItemsProcessed { get; init; }
    public int LearningUpdatesApplied { get; init; }
    public double LearningImprovement { get; init; }
    public TimeSpan ProcessingDuration { get; init; }
    public Dictionary<string, double> ImprovementMetrics { get; init; } = new();
    public DateTime ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Processed feedback
/// </summary>
public record ProcessedFeedback
{
    public string FeedbackId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Impact { get; init; }
    public List<string> AppliedChanges { get; init; } = new();
    public DateTime ProcessedAt { get; init; }
    public Dictionary<string, object> ProcessingData { get; init; } = new();
}
