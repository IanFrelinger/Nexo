using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Core.Application.Interfaces.Services
{
    /// <summary>
    /// Interface for analytics services
    /// </summary>
    public interface IAnalyticsService
    {
        Task TrackEventAsync(string eventName, Dictionary<string, object> properties);
        Task InitializeAsync();
        Task TrackUserActionAsync(string userId, string action, Dictionary<string, object> context);
        Task TrackModelUsageAsync(string modelId, string operation, TimeSpan duration, bool success);
        Task TrackPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object> metrics);
        Task<Dictionary<string, object>> GetAnalyticsAsync(string userId, DateTime from, DateTime to);
        Task<List<AnalyticsEvent>> GetEventsAsync(string userId, DateTime from, DateTime to);
        Task<Dictionary<string, object>> GetMetricsAsync(string metricType, DateTime from, DateTime to);
        Task ReportErrorAsync(string error, Dictionary<string, object> context);
        Task TrackFeatureUsageAsync(string featureName, string userId, Dictionary<string, object> properties);
        Task<Dictionary<string, object>> GetUserInsightsAsync(string userId);
        Task<Dictionary<string, object>> GetUserMetricsAsync(string userId);
        Task<Dictionary<string, object>> GetEngagementMetricsAsync(string userId);
        Task<Dictionary<string, object>> GetFeedbackMetricsAsync(string userId);
        Task<Dictionary<string, object>> GetPerformanceMetricsAsync(string userId);
    }

    /// <summary>
    /// Represents an analytics event
    /// </summary>
    public class AnalyticsEvent
    {
        public string Id { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SessionId { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }
}