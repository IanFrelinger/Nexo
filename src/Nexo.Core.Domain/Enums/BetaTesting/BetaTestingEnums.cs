namespace Nexo.Core.Domain.Enums.BetaTesting
{
    /// <summary>
    /// Status of beta programs
    /// </summary>
    public enum BetaProgramStatus
    {
        Pending,
        Active,
        Paused,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Status of beta user segments
    /// </summary>
    public enum BetaSegmentStatus
    {
        Pending,
        Recruiting,
        Full,
        Active,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Status of beta users
    /// </summary>
    public enum BetaUserStatus
    {
        Pending,
        Invited,
        Active,
        Inactive,
        Completed,
        Dropped
    }

    /// <summary>
    /// Types of beta feedback
    /// </summary>
    public enum BetaFeedbackType
    {
        BugReport,
        FeatureRequest,
        UsabilityIssue,
        PerformanceIssue,
        GeneralFeedback,
        SurveyResponse,
        InterviewResponse
    }

    /// <summary>
    /// Status of beta feedback
    /// </summary>
    public enum BetaFeedbackStatus
    {
        New,
        InReview,
        Processed,
        Resolved,
        Rejected,
        Duplicate
    }

    /// <summary>
    /// Types of recommendations
    /// </summary>
    public enum RecommendationType
    {
        UserRetention,
        Engagement,
        FeatureImprovement,
        Performance,
        HealthImprovement,
        ProcessImprovement
    }

    /// <summary>
    /// Priority levels for recommendations
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Overall program health status
    /// </summary>
    public enum ProgramHealth
    {
        Critical,
        Poor,
        Fair,
        Good,
        Excellent
    }

    /// <summary>
    /// Types of health checks
    /// </summary>
    public enum HealthCheckType
    {
        Recruitment,
        Engagement,
        Feedback,
        SuccessCriteria,
        Performance,
        UserSatisfaction
    }

    /// <summary>
    /// Status of individual health checks
    /// </summary>
    public enum HealthStatus
    {
        Critical,
        Poor,
        Fair,
        Good,
        Healthy
    }

    /// <summary>
    /// Types of analytics events
    /// </summary>
    public enum BetaAnalyticsEventType
    {
        ProgramInitialized,
        UsersRecruited,
        FeedbackCollected,
        UserEngaged,
        UserDropped,
        FeatureUsed,
        ErrorOccurred,
        SurveyCompleted,
        InterviewCompleted
    }
}
