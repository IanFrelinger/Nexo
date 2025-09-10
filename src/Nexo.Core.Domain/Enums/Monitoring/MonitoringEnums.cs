namespace Nexo.Core.Domain.Enums.Monitoring
{
    /// <summary>
    /// Health status levels
    /// </summary>
    public enum HealthStatus
    {
        Critical,
        Poor,
        Fair,
        Good,
        Excellent
    }

    /// <summary>
    /// Types of alerts
    /// </summary>
    public enum AlertType
    {
        MetricThreshold,
        HealthCheck,
        Error,
        Performance,
        Security,
        Capacity,
        Availability
    }

    /// <summary>
    /// Alert severity levels
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// Alert status
    /// </summary>
    public enum AlertStatus
    {
        New,
        Acknowledged,
        Resolved,
        Suppressed
    }

    /// <summary>
    /// Types of alert channels
    /// </summary>
    public enum AlertChannelType
    {
        Email,
        Slack,
        Teams,
        Webhook,
        SMS,
        Phone
    }

    /// <summary>
    /// Types of insights
    /// </summary>
    public enum InsightType
    {
        Performance,
        Capacity,
        Security,
        Usage,
        Error,
        Trend
    }

    /// <summary>
    /// Insight severity levels
    /// </summary>
    public enum InsightSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// Types of recommendations
    /// </summary>
    public enum RecommendationType
    {
        Performance,
        Capacity,
        Security,
        Cost,
        Reliability,
        HealthImprovement
    }

    /// <summary>
    /// Recommendation priority levels
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
