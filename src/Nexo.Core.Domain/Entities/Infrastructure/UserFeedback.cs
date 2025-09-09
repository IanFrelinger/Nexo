using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// User feedback types
/// </summary>
public enum FeedbackType
{
    Performance,
    Usability,
    Feature,
    Bug,
    General
}

/// <summary>
/// User feedback
/// </summary>
public record UserFeedback
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of feedback
    /// </summary>
    public FeedbackType Type { get; init; }
    
    /// <summary>
    /// Feedback message
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Content (alias for Message)
    /// </summary>
    public string Content => Message;
    
    /// <summary>
    /// Severity of feedback
    /// </summary>
    public FeedbackSeverity Severity { get; init; } = FeedbackSeverity.Medium;
    
    /// <summary>
    /// User identifier
    /// </summary>
    public string UserId { get; init; } = string.Empty;
    
    /// <summary>
    /// Timestamp when feedback was provided
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Rating (1-10 scale)
    /// </summary>
    public int Rating { get; init; } = 5;
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Comment (alias for Message)
    /// </summary>
    public string Comment => Message;
    
    /// <summary>
    /// Context information
    /// </summary>
    public string Context { get; init; } = string.Empty;
}

/// <summary>
/// Negative feedback event arguments
/// </summary>
public class NegativeFeedbackEventArgs : EventArgs
{
    /// <summary>
    /// User feedback
    /// </summary>
    public UserFeedback Feedback { get; }
    
    /// <summary>
    /// Adaptation context
    /// </summary>
    public AdaptationContext Context { get; }
    
    /// <summary>
    /// Constructor
    /// </summary>
    public NegativeFeedbackEventArgs(UserFeedback feedback, AdaptationContext context)
    {
        Feedback = feedback;
        Context = context;
    }
}

/// <summary>
/// Feedback analysis result
/// </summary>
public record FeedbackAnalysisResult
{
    /// <summary>
    /// Sentiment score (-1.0 to 1.0)
    /// </summary>
    public double SentimentScore { get; init; } = 0.0;
    
    /// <summary>
    /// Key themes identified
    /// </summary>
    public List<string> KeyThemes { get; init; } = new();
    
    /// <summary>
    /// Recommended actions
    /// </summary>
    public List<string> RecommendedActions { get; init; } = new();
    
    /// <summary>
    /// Priority level
    /// </summary>
    public AdaptationPriority Priority { get; init; } = AdaptationPriority.Normal;
    
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Feedback analysis
/// </summary>
public record FeedbackAnalysis
{
    /// <summary>
    /// Analysis result
    /// </summary>
    public FeedbackAnalysisResult Result { get; init; } = new();
    
    /// <summary>
    /// Confidence in analysis
    /// </summary>
    public double Confidence { get; init; } = 0.0;
    
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}