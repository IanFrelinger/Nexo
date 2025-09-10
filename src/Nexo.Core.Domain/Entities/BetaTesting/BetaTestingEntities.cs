using Nexo.Core.Domain.Enums.BetaTesting;

namespace Nexo.Core.Domain.Entities.BetaTesting
{
    /// <summary>
    /// Represents a beta testing program
    /// </summary>
    public class BetaProgram
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public BetaProgramStatus Status { get; set; } = BetaProgramStatus.Pending;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public BetaProgramConfiguration Configuration { get; set; } = new();
        public List<BetaUserSegment> Segments { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a beta testing program
    /// </summary>
    public class BetaProgramConfiguration
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DurationDays { get; set; } = 30;
        public List<BetaUserSegmentConfiguration> UserSegments { get; set; } = new();
        public List<string> FocusAreas { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a user segment in the beta program
    /// </summary>
    public class BetaUserSegmentConfiguration
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int TargetSize { get; set; }
        public List<string> FocusAreas { get; set; } = new();
        public List<string> Requirements { get; set; } = new();
        public Dictionary<string, object> Criteria { get; set; } = new();
    }

    /// <summary>
    /// User segment within a beta program
    /// </summary>
    public class BetaUserSegment
    {
        public string Id { get; set; } = "";
        public string ProgramId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int TargetSize { get; set; }
        public int CurrentSize { get; set; }
        public List<string> FocusAreas { get; set; } = new();
        public BetaSegmentStatus Status { get; set; } = BetaSegmentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<BetaUser> Users { get; set; } = new();
    }

    /// <summary>
    /// Beta testing user
    /// </summary>
    public class BetaUser
    {
        public string Id { get; set; } = "";
        public string ProgramId { get; set; } = "";
        public string SegmentId { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string ExperienceLevel { get; set; } = "";
        public List<string> Interests { get; set; } = new();
        public BetaUserStatus Status { get; set; } = BetaUserStatus.Pending;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActiveAt { get; set; }
        public Dictionary<string, object> Profile { get; set; } = new();
    }

    /// <summary>
    /// Feedback from beta users
    /// </summary>
    public class BetaFeedback
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProgramId { get; set; } = "";
        public string UserId { get; set; } = "";
        public BetaFeedbackType Type { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public int Rating { get; set; }
        public List<string> Tags { get; set; } = new();
        public BetaFeedbackStatus Status { get; set; } = BetaFeedbackStatus.New;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of beta program operations
    /// </summary>
    public class BetaProgramResult
    {
        public string ProgramId { get; set; } = "";
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of user recruitment
    /// </summary>
    public class RecruitmentResult
    {
        public string ProgramId { get; set; } = "";
        public List<BetaUser> RecruitedUsers { get; set; } = new();
        public int TotalRecruited { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request for user recruitment
    /// </summary>
    public class RecruitmentRequest
    {
        public List<string> SegmentIds { get; set; } = new();
        public RecruitmentCriteria RecruitmentCriteria { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Criteria for recruiting users
    /// </summary>
    public class RecruitmentCriteria
    {
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> PreferredExperience { get; set; } = new();
        public string? GeographicRegion { get; set; }
        public List<string> Industries { get; set; } = new();
        public Dictionary<string, object> CustomCriteria { get; set; } = new();
    }

    /// <summary>
    /// Result of feedback collection
    /// </summary>
    public class FeedbackCollectionResult
    {
        public string ProgramId { get; set; } = "";
        public List<BetaFeedback> CollectedFeedback { get; set; } = new();
        public int TotalCollected { get; set; }
        public FeedbackAnalysis? Analysis { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request for feedback collection
    /// </summary>
    public class FeedbackCollectionRequest
    {
        public bool IncludeInAppFeedback { get; set; } = true;
        public bool IncludeSurveyFeedback { get; set; } = true;
        public bool IncludeInterviewFeedback { get; set; } = false;
        public string? SurveyId { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Analysis of collected feedback
    /// </summary>
    public class FeedbackAnalysis
    {
        public int TotalFeedback { get; set; }
        public double PositiveSentiment { get; set; }
        public double NegativeSentiment { get; set; }
        public double NeutralSentiment { get; set; }
        public List<string> TopIssues { get; set; } = new();
        public List<string> TopFeatures { get; set; } = new();
        public double SatisfactionScore { get; set; }
        public int NetPromoterScore { get; set; }
        public Dictionary<string, object> Insights { get; set; } = new();
    }

    /// <summary>
    /// Analytics report for beta program
    /// </summary>
    public class BetaAnalyticsReport
    {
        public string ProgramId { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
        public DateRange DateRange { get; set; } = new();
        public UserMetrics UserMetrics { get; set; } = new();
        public EngagementMetrics EngagementMetrics { get; set; } = new();
        public FeedbackMetrics FeedbackMetrics { get; set; } = new();
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Request for analytics report
    /// </summary>
    public class AnalyticsReportRequest
    {
        public DateRange DateRange { get; set; } = new();
        public List<string> Metrics { get; set; } = new();
        public bool IncludeRecommendations { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Date range for analytics
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// User metrics for analytics
    /// </summary>
    public class UserMetrics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public double RetentionRate { get; set; }
        public double ChurnRate { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public Dictionary<string, object> Demographics { get; set; } = new();
    }

    /// <summary>
    /// Engagement metrics for analytics
    /// </summary>
    public class EngagementMetrics
    {
        public int TotalSessions { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public double FeatureUsageRate { get; set; }
        public int PageViews { get; set; }
        public double BounceRate { get; set; }
        public Dictionary<string, object> EngagementData { get; set; } = new();
    }

    /// <summary>
    /// Feedback metrics for analytics
    /// </summary>
    public class FeedbackMetrics
    {
        public int TotalFeedback { get; set; }
        public double ResponseRate { get; set; }
        public double AverageRating { get; set; }
        public double SatisfactionScore { get; set; }
        public int NetPromoterScore { get; set; }
        public Dictionary<string, object> FeedbackData { get; set; } = new();
    }

    /// <summary>
    /// Performance metrics for analytics
    /// </summary>
    public class PerformanceMetrics
    {
        public double AverageResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public double Uptime { get; set; }
        public int TotalRequests { get; set; }
        public Dictionary<string, object> PerformanceData { get; set; } = new();
    }

    /// <summary>
    /// Recommendation for improving the beta program
    /// </summary>
    public class Recommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> ActionItems { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Program health report
    /// </summary>
    public class ProgramHealthReport
    {
        public string ProgramId { get; set; } = "";
        public ProgramHealth OverallHealth { get; set; }
        public List<HealthCheckResult> HealthChecks { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of a health check
    /// </summary>
    public class HealthCheckResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public HealthCheckType CheckType { get; set; }
        public HealthStatus Status { get; set; }
        public double Score { get; set; }
        public string Message { get; set; } = "";
        public Dictionary<string, object> Details { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Analytics event for tracking
    /// </summary>
    public class BetaAnalyticsEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public BetaAnalyticsEventType EventType { get; set; }
        public string ProgramId { get; set; } = "";
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
