using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for user experience analysis
/// </summary>
public interface IUserExperienceAnalyzer
{
    /// <summary>
    /// Analyze user experience based on feedback and metrics
    /// </summary>
    Task<UserExperienceAnalysis> AnalyzeUserExperienceAsync(IEnumerable<UserFeedback> feedback, IEnumerable<PerformanceData> metrics);
    
    /// <summary>
    /// Analyze user experience trends
    /// </summary>
    Task<UserExperienceTrend> AnalyzeUserExperienceTrendAsync(TimeSpan timeWindow);
    
    /// <summary>
    /// Get user experience score
    /// </summary>
    Task<UserExperienceScore> GetUserExperienceScoreAsync();
    
    /// <summary>
    /// Identify user experience issues
    /// </summary>
    Task<IEnumerable<UserExperienceIssue>> IdentifyIssuesAsync();
    
    /// <summary>
    /// Get user experience recommendations
    /// </summary>
    Task<IEnumerable<UserExperienceRecommendation>> GetRecommendationsAsync();
    
    /// <summary>
    /// Track user experience metrics
    /// </summary>
    Task TrackUserExperienceMetricAsync(UserExperienceMetric metric);
}

/// <summary>
/// User experience analysis result
/// </summary>
public record UserExperienceAnalysis
{
    /// <summary>
    /// Overall user experience score (0-100)
    /// </summary>
    public double OverallScore { get; init; }
    
    /// <summary>
    /// Performance score (0-100)
    /// </summary>
    public double PerformanceScore { get; init; }
    
    /// <summary>
    /// Usability score (0-100)
    /// </summary>
    public double UsabilityScore { get; init; }
    
    /// <summary>
    /// Satisfaction score (0-100)
    /// </summary>
    public double SatisfactionScore { get; init; }
    
    /// <summary>
    /// Key issues identified
    /// </summary>
    public List<string> KeyIssues { get; init; } = new();
    
    /// <summary>
    /// Positive aspects
    /// </summary>
    public List<string> PositiveAspects { get; init; } = new();
    
    /// <summary>
    /// Recommendations for improvement
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// User experience trend
/// </summary>
public record UserExperienceTrend
{
    /// <summary>
    /// Trend direction
    /// </summary>
    public TrendDirection Direction { get; init; }
    
    /// <summary>
    /// Trend strength (0-1)
    /// </summary>
    public double Strength { get; init; }
    
    /// <summary>
    /// Rate of change per day
    /// </summary>
    public double RateOfChangePerDay { get; init; }
    
    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Time window analyzed
    /// </summary>
    public TimeSpan TimeWindow { get; init; }
}

/// <summary>
/// User experience score
/// </summary>
public record UserExperienceScore
{
    /// <summary>
    /// Overall score (0-100)
    /// </summary>
    public double OverallScore { get; init; }
    
    /// <summary>
    /// Performance score (0-100)
    /// </summary>
    public double PerformanceScore { get; init; }
    
    /// <summary>
    /// Usability score (0-100)
    /// </summary>
    public double UsabilityScore { get; init; }
    
    /// <summary>
    /// Satisfaction score (0-100)
    /// </summary>
    public double SatisfactionScore { get; init; }
    
    /// <summary>
    /// Score timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Score confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// User experience issue
/// </summary>
public record UserExperienceIssue
{
    /// <summary>
    /// Issue identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Issue type
    /// </summary>
    public UserExperienceIssueType Type { get; init; }
    
    /// <summary>
    /// Issue severity
    /// </summary>
    public IssueSeverity Severity { get; init; }
    
    /// <summary>
    /// Issue description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Affected user count
    /// </summary>
    public int AffectedUserCount { get; init; }
    
    /// <summary>
    /// Issue frequency
    /// </summary>
    public double Frequency { get; init; }
    
    /// <summary>
    /// Issue timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// User experience issue types
/// </summary>
public enum UserExperienceIssueType
{
    /// <summary>
    /// Performance issue
    /// </summary>
    Performance,
    
    /// <summary>
    /// Usability issue
    /// </summary>
    Usability,
    
    /// <summary>
    /// Accessibility issue
    /// </summary>
    Accessibility,
    
    /// <summary>
    /// Functionality issue
    /// </summary>
    Functionality,
    
    /// <summary>
    /// Design issue
    /// </summary>
    Design
}

/// <summary>
/// Issue severity levels
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Low severity
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium severity
    /// </summary>
    Medium,
    
    /// <summary>
    /// High severity
    /// </summary>
    High,
    
    /// <summary>
    /// Critical severity
    /// </summary>
    Critical
}

/// <summary>
/// User experience recommendation
/// </summary>
public record UserExperienceRecommendation
{
    /// <summary>
    /// Recommendation identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Recommendation type
    /// </summary>
    public UserExperienceIssueType Type { get; init; }
    
    /// <summary>
    /// Recommendation priority
    /// </summary>
    public int Priority { get; init; }
    
    /// <summary>
    /// Recommendation description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Expected impact
    /// </summary>
    public string ExpectedImpact { get; init; } = string.Empty;
    
    /// <summary>
    /// Implementation effort
    /// </summary>
    public ImplementationEffort Effort { get; init; }
    
    /// <summary>
    /// Recommendation timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Implementation effort levels
/// </summary>
public enum ImplementationEffort
{
    /// <summary>
    /// Low effort
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium effort
    /// </summary>
    Medium,
    
    /// <summary>
    /// High effort
    /// </summary>
    High,
    
    /// <summary>
    /// Very high effort
    /// </summary>
    VeryHigh
}

/// <summary>
/// User experience metric
/// </summary>
public record UserExperienceMetric
{
    /// <summary>
    /// Metric identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Metric name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Metric value
    /// </summary>
    public double Value { get; init; }
    
    /// <summary>
    /// Metric unit
    /// </summary>
    public string Unit { get; init; } = string.Empty;
    
    /// <summary>
    /// Metric timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// User identifier (if available)
    /// </summary>
    public string? UserId { get; init; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}