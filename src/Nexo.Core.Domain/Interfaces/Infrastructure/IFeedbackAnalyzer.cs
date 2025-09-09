using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for feedback analysis
/// </summary>
public interface IFeedbackAnalyzer
{
    /// <summary>
    /// Analyze a single feedback item
    /// </summary>
    Task<FeedbackAnalysisResult> AnalyzeFeedbackAsync(UserFeedback feedback);
    
    /// <summary>
    /// Analyze a batch of feedback items
    /// </summary>
    Task<FeedbackAnalysisResult> AnalyzeFeedbackBatchAsync(IEnumerable<UserFeedback> feedback);
    
    /// <summary>
    /// Check if feedback requires immediate action
    /// </summary>
    Task<bool> RequiresImmediateActionAsync(UserFeedback feedback);
    
    /// <summary>
    /// Get feedback sentiment analysis
    /// </summary>
    Task<SentimentAnalysis> GetSentimentAnalysisAsync(UserFeedback feedback);
    
    /// <summary>
    /// Get feedback categorization
    /// </summary>
    Task<FeedbackCategorization> GetCategorizationAsync(UserFeedback feedback);
    
    /// <summary>
    /// Get feedback priority assessment
    /// </summary>
    Task<FeedbackPriority> GetPriorityAssessmentAsync(UserFeedback feedback);
    
    /// <summary>
    /// Get feedback trends analysis
    /// </summary>
    Task<FeedbackTrendsAnalysis> GetTrendsAnalysisAsync(IEnumerable<UserFeedback> feedback, TimeSpan timeWindow);
}

/// <summary>
/// Feedback analysis result
/// </summary>
public record FeedbackAnalysisResult
{
    /// <summary>
    /// Analysis identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Analyzed feedback
    /// </summary>
    public UserFeedback Feedback { get; init; } = new();
    
    /// <summary>
    /// Sentiment analysis
    /// </summary>
    public SentimentAnalysis Sentiment { get; init; } = new();
    
    /// <summary>
    /// Categorization
    /// </summary>
    public FeedbackCategorization Categorization { get; init; } = new();
    
    /// <summary>
    /// Priority assessment
    /// </summary>
    public FeedbackPriority Priority { get; init; } = new();
    
    /// <summary>
    /// Key themes identified
    /// </summary>
    public List<string> KeyThemes { get; init; } = new();
    
    /// <summary>
    /// Action recommendations
    /// </summary>
    public List<string> ActionRecommendations { get; init; } = new();
    
    /// <summary>
    /// Analysis confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Sentiment analysis result
/// </summary>
public record SentimentAnalysis
{
    /// <summary>
    /// Overall sentiment score (-1 to 1)
    /// </summary>
    public double SentimentScore { get; init; }
    
    /// <summary>
    /// Sentiment label
    /// </summary>
    public SentimentLabel Label { get; init; }
    
    /// <summary>
    /// Sentiment confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Positive sentiment score (0-1)
    /// </summary>
    public double PositiveScore { get; init; }
    
    /// <summary>
    /// Negative sentiment score (0-1)
    /// </summary>
    public double NegativeScore { get; init; }
    
    /// <summary>
    /// Neutral sentiment score (0-1)
    /// </summary>
    public double NeutralScore { get; init; }
    
    /// <summary>
    /// Emotional intensity (0-1)
    /// </summary>
    public double EmotionalIntensity { get; init; }
}

/// <summary>
/// Sentiment labels
/// </summary>
public enum SentimentLabel
{
    /// <summary>
    /// Very negative sentiment
    /// </summary>
    VeryNegative,
    
    /// <summary>
    /// Negative sentiment
    /// </summary>
    Negative,
    
    /// <summary>
    /// Neutral sentiment
    /// </summary>
    Neutral,
    
    /// <summary>
    /// Positive sentiment
    /// </summary>
    Positive,
    
    /// <summary>
    /// Very positive sentiment
    /// </summary>
    VeryPositive
}

/// <summary>
/// Feedback categorization
/// </summary>
public record FeedbackCategorization
{
    /// <summary>
    /// Primary category
    /// </summary>
    public string PrimaryCategory { get; init; } = string.Empty;
    
    /// <summary>
    /// Secondary categories
    /// </summary>
    public List<string> SecondaryCategories { get; init; } = new();
    
    /// <summary>
    /// Category confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Category scores
    /// </summary>
    public Dictionary<string, double> CategoryScores { get; init; } = new();
    
    /// <summary>
    /// Category keywords
    /// </summary>
    public List<string> Keywords { get; init; } = new();
}

/// <summary>
/// Feedback priority assessment
/// </summary>
public record FeedbackPriority
{
    /// <summary>
    /// Priority level (1-10)
    /// </summary>
    public int Level { get; init; }
    
    /// <summary>
    /// Priority label
    /// </summary>
    public PriorityLabel Label { get; init; }
    
    /// <summary>
    /// Priority confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Factors influencing priority
    /// </summary>
    public List<string> InfluencingFactors { get; init; } = new();
    
    /// <summary>
    /// Urgency level
    /// </summary>
    public UrgencyLevel Urgency { get; init; }
    
    /// <summary>
    /// Impact assessment
    /// </summary>
    public ImpactAssessment Impact { get; init; } = new();
}

/// <summary>
/// Priority labels
/// </summary>
public enum PriorityLabel
{
    /// <summary>
    /// Low priority
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium priority
    /// </summary>
    Medium,
    
    /// <summary>
    /// High priority
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority
    /// </summary>
    Critical
}

/// <summary>
/// Urgency levels
/// </summary>
public enum UrgencyLevel
{
    /// <summary>
    /// Low urgency
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium urgency
    /// </summary>
    Medium,
    
    /// <summary>
    /// High urgency
    /// </summary>
    High,
    
    /// <summary>
    /// Immediate urgency
    /// </summary>
    Immediate
}

/// <summary>
/// Impact assessment
/// </summary>
public record ImpactAssessment
{
    /// <summary>
    /// User impact level (1-10)
    /// </summary>
    public int UserImpact { get; init; }
    
    /// <summary>
    /// Business impact level (1-10)
    /// </summary>
    public int BusinessImpact { get; init; }
    
    /// <summary>
    /// Technical impact level (1-10)
    /// </summary>
    public int TechnicalImpact { get; init; }
    
    /// <summary>
    /// Overall impact level (1-10)
    /// </summary>
    public int OverallImpact { get; init; }
    
    /// <summary>
    /// Impact description
    /// </summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Feedback trends analysis
/// </summary>
public record FeedbackTrendsAnalysis
{
    /// <summary>
    /// Analysis identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Time window analyzed
    /// </summary>
    public TimeSpan TimeWindow { get; init; }
    
    /// <summary>
    /// Total feedback analyzed
    /// </summary>
    public int TotalFeedback { get; init; }
    
    /// <summary>
    /// Sentiment trends
    /// </summary>
    public Dictionary<SentimentLabel, int> SentimentTrends { get; init; } = new();
    
    /// <summary>
    /// Category trends
    /// </summary>
    public Dictionary<string, int> CategoryTrends { get; init; } = new();
    
    /// <summary>
    /// Priority trends
    /// </summary>
    public Dictionary<PriorityLabel, int> PriorityTrends { get; init; } = new();
    
    /// <summary>
    /// Volume trends over time
    /// </summary>
    public Dictionary<DateTime, int> VolumeTrends { get; init; } = new();
    
    /// <summary>
    /// Key insights
    /// </summary>
    public List<string> KeyInsights { get; init; } = new();
    
    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}