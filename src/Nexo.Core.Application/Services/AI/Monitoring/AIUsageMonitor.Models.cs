using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Services.AI.Monitoring
{
    /// <summary>
    /// AI usage monitoring data models
    /// </summary>
    public partial class AIUsageMonitor
    {
        // Data models are defined as separate classes below
    }

    /// <summary>
    /// AI usage session tracking
    /// </summary>
    public class AIUsageSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public AIOperationStatus Status { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public AIOperationContext? Context { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public Dictionary<string, object> Results { get; set; } = new();
        public List<AIUsageEvent> Events { get; set; } = new();
    }

    /// <summary>
    /// AI usage event
    /// </summary>
    public class AIUsageEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public AIUsageEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// AI usage event types
    /// </summary>
    public enum AIUsageEventType
    {
        OperationStarted,
        OperationUpdated,
        OperationCompleted,
        OperationFailed,
        OperationCancelled,
        CustomEvent
    }

    /// <summary>
    /// AI usage statistics
    /// </summary>
    public class AIUsageStatistics
    {
        public TimeSpan? TimeRange { get; set; }
        public string? UserId { get; set; }
        public int TotalEvents { get; set; }
        public int TotalOperations { get; set; }
        public int CompletedOperations { get; set; }
        public int FailedOperations { get; set; }
        public TimeSpan AverageOperationDuration { get; set; }
        public string MostUsedOperationType { get; set; } = string.Empty;
        public string MostUsedPlatform { get; set; } = string.Empty;
        public double SuccessRate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// AI usage analytics
    /// </summary>
    public class AIUsageAnalytics
    {
        public AIUsageStatistics Statistics { get; set; } = new();
        public int ActiveOperations { get; set; }
        public DateTime PeakUsageTime { get; set; }
        public List<string> UsageTrends { get; set; } = new();
        public List<string> PerformanceInsights { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
