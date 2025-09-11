using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Monitoring
{
    /// <summary>
    /// AI usage monitoring service for tracking and analyzing AI operations
    /// </summary>
    public class AIUsageMonitor
    {
        private readonly ILogger<AIUsageMonitor> _logger;
        private readonly Dictionary<string, AIUsageSession> _activeSessions;
        private readonly List<AIUsageEvent> _usageHistory;
        private readonly object _lockObject = new object();

        public AIUsageMonitor(ILogger<AIUsageMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = new Dictionary<string, AIUsageSession>();
            _usageHistory = new List<AIUsageEvent>();
        }

        /// <summary>
        /// Starts monitoring an AI operation
        /// </summary>
        public async Task<string> StartOperationAsync(string operationId, AIOperationContext context, string userId = "")
        {
            try
            {
                _logger.LogDebug("Starting AI operation monitoring for {OperationId}", operationId);

                var session = new AIUsageSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    OperationId = operationId,
                    UserId = userId,
                    StartTime = DateTime.UtcNow,
                    Context = context,
                    Status = AIOperationStatus.Running,
                    Events = new List<AIUsageEvent>()
                };

                lock (_lockObject)
                {
                    _activeSessions[operationId] = session;
                }

                // Log operation start event
                await LogUsageEventAsync(new AIUsageEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    SessionId = session.SessionId,
                    OperationId = operationId,
                    EventType = AIUsageEventType.OperationStarted,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    Details = new Dictionary<string, object>
                    {
                        ["OperationType"] = context.OperationType.ToString(),
                        ["TargetPlatform"] = context.TargetPlatform.ToString(),
                        ["MaxTokens"] = context.MaxTokens,
                        ["Temperature"] = context.Temperature
                    }
                });

                _logger.LogInformation("AI operation monitoring started for {OperationId}", operationId);
                return session.SessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start AI operation monitoring for {OperationId}", operationId);
                throw;
            }
        }

        /// <summary>
        /// Updates an AI operation with progress information
        /// </summary>
        public async Task UpdateOperationAsync(string operationId, AIOperationStatus status, Dictionary<string, object>? details = null)
        {
            try
            {
                _logger.LogDebug("Updating AI operation {OperationId} with status {Status}", operationId, status);

                AIUsageSession? session;
                lock (_lockObject)
                {
                    _activeSessions.TryGetValue(operationId, out session);
                }

                if (session == null)
                {
                    _logger.LogWarning("No active session found for operation {OperationId}", operationId);
                    return;
                }

                session.Status = status;
                session.LastUpdateTime = DateTime.UtcNow;

                if (details != null)
                {
                    session.Details = details;
                }

                // Log operation update event
                await LogUsageEventAsync(new AIUsageEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    SessionId = session.SessionId,
                    OperationId = operationId,
                    EventType = AIUsageEventType.OperationUpdated,
                    Timestamp = DateTime.UtcNow,
                    UserId = session.UserId,
                    Details = details ?? new Dictionary<string, object>()
                });

                _logger.LogDebug("AI operation {OperationId} updated with status {Status}", operationId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI operation {OperationId}", operationId);
            }
        }

        /// <summary>
        /// Completes an AI operation and stops monitoring
        /// </summary>
        public async Task CompleteOperationAsync(string operationId, bool success, string? errorMessage = null, Dictionary<string, object>? results = null)
        {
            try
            {
                _logger.LogDebug("Completing AI operation {OperationId} with success {Success}", operationId, success);

                AIUsageSession? session;
                lock (_lockObject)
                {
                    _activeSessions.TryGetValue(operationId, out session);
                    if (session != null)
                    {
                        _activeSessions.Remove(operationId);
                    }
                }

                if (session == null)
                {
                    _logger.LogWarning("No active session found for operation {OperationId}", operationId);
                    return;
                }

                session.Status = success ? AIOperationStatus.Completed : AIOperationStatus.Failed;
                session.EndTime = DateTime.UtcNow;
                session.Duration = session.EndTime.Value - session.StartTime;
                session.Success = success;
                session.ErrorMessage = errorMessage;

                if (results != null)
                {
                    session.Results = results;
                }

                // Log operation completion event
                await LogUsageEventAsync(new AIUsageEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    SessionId = session.SessionId,
                    OperationId = operationId,
                    EventType = success ? AIUsageEventType.OperationCompleted : AIUsageEventType.OperationFailed,
                    Timestamp = DateTime.UtcNow,
                    UserId = session.UserId,
                    Details = new Dictionary<string, object>
                    {
                        ["Success"] = success,
                        ["Duration"] = session.Duration.TotalMilliseconds,
                        ["ErrorMessage"] = errorMessage ?? "",
                        ["Results"] = results ?? new Dictionary<string, object>()
                    }
                });

                _logger.LogInformation("AI operation {OperationId} completed with success {Success} in {Duration}ms", 
                    operationId, success, session.Duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete AI operation {OperationId}", operationId);
            }
        }

        /// <summary>
        /// Logs a custom usage event
        /// </summary>
        public async Task LogUsageEventAsync(AIUsageEvent usageEvent)
        {
            try
            {
                _logger.LogDebug("Logging AI usage event {EventType} for operation {OperationId}", 
                    usageEvent.EventType, usageEvent.OperationId);

                lock (_lockObject)
                {
                    _usageHistory.Add(usageEvent);
                    
                    // Keep only last 10000 events to prevent memory issues
                    if (_usageHistory.Count > 10000)
                    {
                        _usageHistory.RemoveAt(0);
                    }
                }

                // Add to session if it exists
                AIUsageSession? session;
                lock (_lockObject)
                {
                    _activeSessions.TryGetValue(usageEvent.OperationId, out session);
                }

                if (session != null)
                {
                    session.Events.Add(usageEvent);
                }

                await Task.Delay(10); // Simulate async operation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log AI usage event");
            }
        }

        /// <summary>
        /// Gets usage statistics for a specific time period
        /// </summary>
        public async Task<AIUsageStatistics> GetUsageStatisticsAsync(TimeSpan? timeRange = null, string? userId = null)
        {
            try
            {
                _logger.LogDebug("Generating AI usage statistics");

                var cutoffTime = timeRange.HasValue ? DateTime.UtcNow - timeRange.Value : DateTime.MinValue;
                
                List<AIUsageEvent> relevantEvents;
                lock (_lockObject)
                {
                    relevantEvents = _usageHistory
                        .Where(e => e.Timestamp >= cutoffTime)
                        .Where(e => userId == null || e.UserId == userId)
                        .ToList();
                }

                var statistics = new AIUsageStatistics
                {
                    TimeRange = timeRange,
                    UserId = userId,
                    TotalEvents = relevantEvents.Count,
                    TotalOperations = relevantEvents.Count(e => e.EventType == AIUsageEventType.OperationStarted),
                    CompletedOperations = relevantEvents.Count(e => e.EventType == AIUsageEventType.OperationCompleted),
                    FailedOperations = relevantEvents.Count(e => e.EventType == AIUsageEventType.OperationFailed),
                    AverageOperationDuration = CalculateAverageOperationDuration(relevantEvents),
                    MostUsedOperationType = GetMostUsedOperationType(relevantEvents),
                    MostUsedPlatform = GetMostUsedPlatform(relevantEvents),
                    SuccessRate = CalculateSuccessRate(relevantEvents),
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Generated AI usage statistics: {TotalOperations} operations, {SuccessRate}% success rate", 
                    statistics.TotalOperations, statistics.SuccessRate);

                await Task.Delay(10);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI usage statistics");
                throw;
            }
        }

        /// <summary>
        /// Gets active AI operations
        /// </summary>
        public async Task<List<AIUsageSession>> GetActiveOperationsAsync()
        {
            try
            {
                List<AIUsageSession> activeSessions;
                lock (_lockObject)
                {
                    activeSessions = _activeSessions.Values.ToList();
                }

                await Task.Delay(10);
                return activeSessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active AI operations");
                throw;
            }
        }

        /// <summary>
        /// Gets usage events for a specific operation
        /// </summary>
        public async Task<List<AIUsageEvent>> GetOperationEventsAsync(string operationId)
        {
            try
            {
                List<AIUsageEvent> events;
                lock (_lockObject)
                {
                    events = _usageHistory
                        .Where(e => e.OperationId == operationId)
                        .OrderBy(e => e.Timestamp)
                        .ToList();
                }

                await Task.Delay(10);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get events for operation {OperationId}", operationId);
                throw;
            }
        }

        /// <summary>
        /// Generates usage analytics and insights
        /// </summary>
        public async Task<AIUsageAnalytics> GenerateAnalyticsAsync(TimeSpan? timeRange = null, string? userId = null)
        {
            try
            {
                _logger.LogDebug("Generating AI usage analytics");

                var statistics = await GetUsageStatisticsAsync(timeRange, userId);
                var activeOperations = await GetActiveOperationsAsync();

                var analytics = new AIUsageAnalytics
                {
                    Statistics = statistics,
                    ActiveOperations = activeOperations.Count,
                    PeakUsageTime = CalculatePeakUsageTime(statistics),
                    UsageTrends = CalculateUsageTrends(statistics),
                    PerformanceInsights = GeneratePerformanceInsights(statistics),
                    Recommendations = GenerateUsageRecommendations(statistics),
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Generated AI usage analytics with {Insights} insights and {Recommendations} recommendations", 
                    analytics.PerformanceInsights.Count, analytics.Recommendations.Count);

                await Task.Delay(10);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI usage analytics");
                throw;
            }
        }

        private TimeSpan CalculateAverageOperationDuration(List<AIUsageEvent> events)
        {
            var completedEvents = events.Where(e => e.EventType == AIUsageEventType.OperationCompleted).ToList();
            if (!completedEvents.Any())
                return TimeSpan.Zero;

            var totalDuration = completedEvents
                .Where(e => e.Details.ContainsKey("Duration"))
                .Sum(e => Convert.ToDouble(e.Details["Duration"]));

            return TimeSpan.FromMilliseconds(totalDuration / completedEvents.Count);
        }

        private string GetMostUsedOperationType(AIUsageStatistics statistics)
        {
            // In a real implementation, this would analyze the actual data
            return "CodeGeneration";
        }

        private string GetMostUsedPlatform(AIUsageStatistics statistics)
        {
            // In a real implementation, this would analyze the actual data
            return "WebAssembly";
        }

        private double CalculateSuccessRate(List<AIUsageEvent> events)
        {
            var totalOperations = events.Count(e => e.EventType == AIUsageEventType.OperationStarted);
            var completedOperations = events.Count(e => e.EventType == AIUsageEventType.OperationCompleted);
            
            if (totalOperations == 0)
                return 0;

            return (double)completedOperations / totalOperations * 100;
        }

        private DateTime CalculatePeakUsageTime(AIUsageStatistics statistics)
        {
            // In a real implementation, this would calculate actual peak usage time
            return DateTime.UtcNow.AddHours(-2);
        }

        private List<string> CalculateUsageTrends(AIUsageStatistics statistics)
        {
            var trends = new List<string>();

            if (statistics.SuccessRate > 95)
                trends.Add("High success rate indicates stable AI operations");
            
            if (statistics.TotalOperations > 1000)
                trends.Add("High usage volume suggests good adoption");
            
            if (statistics.AverageOperationDuration.TotalSeconds < 5)
                trends.Add("Fast operation times indicate good performance");

            return trends;
        }

        private List<string> GeneratePerformanceInsights(AIUsageStatistics statistics)
        {
            var insights = new List<string>();

            if (statistics.SuccessRate < 90)
                insights.Add("Consider investigating failed operations to improve success rate");

            if (statistics.AverageOperationDuration.TotalSeconds > 30)
                insights.Add("Long operation times may indicate performance issues");

            if (statistics.TotalOperations == 0)
                insights.Add("No AI operations detected - consider promoting AI features");

            return insights;
        }

        private List<string> GenerateUsageRecommendations(AIUsageStatistics statistics)
        {
            var recommendations = new List<string>();

            if (statistics.SuccessRate < 95)
                recommendations.Add("Implement better error handling and retry mechanisms");

            if (statistics.AverageOperationDuration.TotalSeconds > 10)
                recommendations.Add("Consider optimizing AI models or increasing resources");

            if (statistics.TotalOperations > 5000)
                recommendations.Add("Consider implementing rate limiting for high-volume users");

            return recommendations;
        }
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
