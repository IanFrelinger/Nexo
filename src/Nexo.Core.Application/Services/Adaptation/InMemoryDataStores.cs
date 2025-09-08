namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// In-memory implementation of adaptation data store
/// </summary>
public class InMemoryAdaptationDataStore : IAdaptationDataStore
{
    private readonly List<AdaptationRecord> _adaptations = new();
    private readonly List<AppliedAdaptation> _appliedAdaptations = new();
    private readonly List<AdaptationImprovement> _improvements = new();
    private readonly List<LearningInsight> _insights = new();
    private readonly object _lock = new();
    
    public Task RecordAdaptationAsync(AdaptationRecord record)
    {
        lock (_lock)
        {
            _adaptations.Add(record);
        }
        return Task.CompletedTask;
    }
    
    public Task RecordAppliedAdaptationAsync(AppliedAdaptation adaptation)
    {
        lock (_lock)
        {
            _appliedAdaptations.Add(adaptation);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetRecentAdaptationsAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Where(a => a.AppliedAt >= cutoff));
        }
    }
    
    public Task<IEnumerable<AppliedAdaptation>> GetActiveAdaptationsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_appliedAdaptations.Where(a => a.AppliedAt >= DateTime.UtcNow.AddHours(-1)));
        }
    }
    
    public Task<IEnumerable<AdaptationImprovement>> GetRecentImprovementsAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lock)
        {
            return Task.FromResult(_improvements.Where(i => i.MeasuredAt >= cutoff));
        }
    }
    
    public Task<int> GetTotalAdaptationsCountAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Count);
        }
    }
    
    public Task<double> GetOverallEffectivenessAsync()
    {
        lock (_lock)
        {
            if (!_adaptations.Any()) return Task.FromResult(0.0);
            return Task.FromResult(_adaptations.Average(a => a.EffectivenessScore));
        }
    }
    
    public Task StoreInsightAsync(LearningInsight insight)
    {
        lock (_lock)
        {
            _insights.Add(insight);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<LearningInsight>> GetRecentInsightsAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lock)
        {
            return Task.FromResult(_insights.Where(i => i.DiscoveredAt >= cutoff));
        }
    }
}

/// <summary>
/// In-memory implementation of performance data store
/// </summary>
public class InMemoryPerformanceDataStore : IPerformanceDataStore
{
    private readonly List<PerformanceData> _performanceData = new();
    private readonly object _lock = new();
    
    public Task StorePerformanceDataAsync(PerformanceData data)
    {
        lock (_lock)
        {
            _performanceData.Add(data);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<PerformanceData>> GetRecentPerformanceDataAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lock)
        {
            return Task.FromResult(_performanceData.Where(p => p.Timestamp >= cutoff));
        }
    }
    
    public Task<IEnumerable<PerformanceData>> GetHistoricalDataAsync(TimeSpan timeWindow)
    {
        return GetRecentPerformanceDataAsync(timeWindow);
    }
}

/// <summary>
/// In-memory implementation of feedback store
/// </summary>
public class InMemoryFeedbackStore : IFeedbackStore
{
    private readonly List<UserFeedback> _feedback = new();
    private readonly object _lock = new();
    
    public Task StoreFeedbackAsync(UserFeedback feedback)
    {
        lock (_lock)
        {
            _feedback.Add(feedback);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<UserFeedback>> GetFeedbackSinceAsync(DateTime since)
    {
        lock (_lock)
        {
            return Task.FromResult(_feedback.Where(f => f.Timestamp >= since));
        }
    }
    
    public Task<IEnumerable<UserFeedback>> GetFeedbackInTimeRangeAsync(DateTime start, DateTime end)
    {
        lock (_lock)
        {
            return Task.FromResult(_feedback.Where(f => f.Timestamp >= start && f.Timestamp <= end));
        }
    }
    
    public Task<UserFeedback?> GetFeedbackByIdAsync(string feedbackId)
    {
        lock (_lock)
        {
            return Task.FromResult(_feedback.FirstOrDefault(f => f.FeedbackId == feedbackId));
        }
    }
    
    public Task DeleteFeedbackAsync(string feedbackId)
    {
        lock (_lock)
        {
            _feedback.RemoveAll(f => f.FeedbackId == feedbackId);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory implementation of environment data store
/// </summary>
public class InMemoryEnvironmentDataStore : IEnvironmentDataStore
{
    private DetectedEnvironment? _lastEnvironment;
    private readonly List<EnvironmentChange> _changes = new();
    private readonly object _lock = new();
    
    public Task StoreEnvironmentAsync(DetectedEnvironment environment)
    {
        lock (_lock)
        {
            _lastEnvironment = environment;
        }
        return Task.CompletedTask;
    }
    
    public Task<DetectedEnvironment?> GetLastDetectedEnvironmentAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_lastEnvironment);
        }
    }
    
    public Task RecordEnvironmentChangeAsync(EnvironmentChange change)
    {
        lock (_lock)
        {
            _changes.Add(change);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<EnvironmentChange>> GetEnvironmentChangeHistoryAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lock)
        {
            return Task.FromResult(_changes.Where(c => c.ChangedAt >= cutoff));
        }
    }
}
