using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Application.Services.Environment;

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
    
    public Task<IEnumerable<LearningInsight>> GetRecentInsightsAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lock)
        {
            return Task.FromResult(_insights.Where(i => i.DiscoveredAt >= cutoff));
        }
    }
    
    public Task StoreAdaptationRecordAsync(AdaptationRecord record)
    {
        return RecordAdaptationAsync(record);
    }
    
    public Task<AdaptationRecord?> GetAdaptationRecordAsync(string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.FirstOrDefault(a => a.Id == id));
        }
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetAdaptationRecordsAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Where(a => a.AppliedAt >= startTime && a.AppliedAt <= endTime));
        }
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetAdaptationRecordsByTypeAsync(AdaptationType type)
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Where(a => a.Type == type));
        }
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetSuccessfulAdaptationsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Where(a => a.Success));
        }
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetFailedAdaptationsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Where(a => !a.Success));
        }
    }
    
    public Task UpdateAdaptationRecordAsync(AdaptationRecord record)
    {
        lock (_lock)
        {
            var index = _adaptations.FindIndex(a => a.Id == record.Id);
            if (index >= 0)
            {
                _adaptations[index] = record;
            }
        }
        return Task.CompletedTask;
    }
    
    public Task DeleteAdaptationRecordAsync(string id)
    {
        lock (_lock)
        {
            _adaptations.RemoveAll(a => a.Id == id);
        }
        return Task.CompletedTask;
    }
    
    // IAdaptationDataStore interface methods
    public Task StoreAdaptationAsync(AdaptationRecord adaptation)
    {
        return RecordAdaptationAsync(adaptation);
    }
    
    
    
    
    public Task StoreImprovementAsync(AdaptationImprovement improvement)
    {
        lock (_lock)
        {
            _improvements.Add(improvement);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<AdaptationImprovement>> GetImprovementsAsync(string adaptationId)
    {
        lock (_lock)
        {
            var improvements = _improvements.Where(i => i.AdaptationId == adaptationId);
            return Task.FromResult(improvements);
        }
    }
    
    public Task<IEnumerable<LearningInsight>> GetInsightsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_insights.AsEnumerable());
        }
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetActiveAdaptationsAsync()
    {
        lock (_lock)
        {
            var active = _adaptations.Where(a => a.Success);
            return Task.FromResult(active);
        }
    }
    
    public Task<IEnumerable<AdaptationImprovement>> GetRecentImprovementsAsync(int count = 10)
    {
        lock (_lock)
        {
            var recent = _improvements.OrderByDescending(i => i.AppliedAt).Take(count);
            return Task.FromResult(recent);
        }
    }
    
    public Task<double> GetOverallEffectivenessAsync()
    {
        lock (_lock)
        {
            if (!_adaptations.Any())
                return Task.FromResult(0.0);
                
            var avgEffectiveness = _adaptations.Average(a => a.EffectivenessScore);
            return Task.FromResult(avgEffectiveness);
        }
    }
    
    public Task DeleteOldDataAsync(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _adaptations.RemoveAll(a => a.Timestamp < cutoffTime);
            _appliedAdaptations.RemoveAll(a => a.AppliedAt < cutoffTime);
            _improvements.RemoveAll(i => i.AppliedAt < cutoffTime);
            _insights.RemoveAll(i => i.Timestamp < cutoffTime);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetAdaptationsAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Where(a => a.AppliedAt >= startTime && a.AppliedAt <= endTime));
        }
    }
    
    public Task<AdaptationRecord?> GetAdaptationAsync(string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.FirstOrDefault(a => a.Id == id));
        }
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetAdaptationsByTypeAsync(string type)
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.Where(a => a.Type.ToString() == type));
        }
    }
    
    
    public Task UpdateAdaptationAsync(AdaptationRecord record)
    {
        lock (_lock)
        {
            var existing = _adaptations.FirstOrDefault(a => a.Id == record.Id);
            if (existing != null)
            {
                _adaptations.Remove(existing);
                _adaptations.Add(record);
            }
        }
        return Task.CompletedTask;
    }
    
    public Task DeleteAdaptationAsync(string id)
    {
        lock (_lock)
        {
            _adaptations.RemoveAll(a => a.Id == id);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<LearningInsight>> GetInsightsAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return Task.FromResult(_insights.Where(i => i.Timestamp >= startTime && i.Timestamp <= endTime));
        }
    }
    
    public Task<LearningInsight?> GetInsightAsync(string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_insights.FirstOrDefault(i => i.Id == id));
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
    
    public Task UpdateInsightAsync(LearningInsight insight)
    {
        lock (_lock)
        {
            var existing = _insights.FirstOrDefault(i => i.Id == insight.Id);
            if (existing != null)
            {
                _insights.Remove(existing);
                _insights.Add(insight);
            }
        }
        return Task.CompletedTask;
    }
    
    public Task DeleteInsightAsync(string id)
    {
        lock (_lock)
        {
            _insights.RemoveAll(i => i.Id == id);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<AdaptationRecord>> GetRecentAdaptationsAsync(int count)
    {
        lock (_lock)
        {
            return Task.FromResult(_adaptations.OrderByDescending(a => a.AppliedAt).Take(count));
        }
    }
    
    public Task<IEnumerable<LearningInsight>> GetRecentInsightsAsync(int count)
    {
        lock (_lock)
        {
            return Task.FromResult(_insights.OrderByDescending(i => i.Timestamp).Take(count));
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
    
    public Task StorePerformanceDataAsync(IEnumerable<PerformanceData> data)
    {
        lock (_lock)
        {
            _performanceData.AddRange(data);
        }
        return Task.CompletedTask;
    }
    
    public Task<PerformanceData?> GetPerformanceDataAsync(string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_performanceData.FirstOrDefault(p => p.Id == id));
        }
    }
    
    public Task<IEnumerable<PerformanceData>> GetPerformanceDataAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return Task.FromResult(_performanceData.Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime));
        }
    }
    
    public Task<IEnumerable<PerformanceData>> GetPerformanceDataByMetricAsync(string metricName, DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return Task.FromResult(_performanceData.Where(p => p.MetricName == metricName && p.Timestamp >= startTime && p.Timestamp <= endTime));
        }
    }
    
    public Task<PerformanceData?> GetLatestPerformanceDataAsync(string metricName)
    {
        lock (_lock)
        {
            return Task.FromResult(_performanceData.Where(p => p.MetricName == metricName).OrderByDescending(p => p.Timestamp).FirstOrDefault());
        }
    }
    
    public Task<PerformanceDataSummary> GetPerformanceDataSummaryAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            var data = _performanceData.Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime);
            return Task.FromResult(new PerformanceDataSummary
            {
                TotalRecords = data.Count(),
                AverageValue = data.Average(p => p.Value),
                MinValue = data.Min(p => p.Value),
                MaxValue = data.Max(p => p.Value),
                StartTime = startTime,
                EndTime = endTime
            });
        }
    }
    
    public Task DeleteOldPerformanceDataAsync(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _performanceData.RemoveAll(p => p.Timestamp < cutoffTime);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<PerformanceData>> GetHistoricalDataAsync(TimeSpan timeWindow)
    {
        return GetRecentPerformanceDataAsync(timeWindow);
    }
    
    // IPerformanceDataStore interface methods
    public Task<IEnumerable<PerformanceData>> GetRecentPerformanceDataAsync(int count = 10)
    {
        lock (_lock)
        {
            var recent = _performanceData.OrderByDescending(p => p.Timestamp).Take(count);
            return Task.FromResult(recent);
        }
    }
    
    public Task<PerformanceDataSummary> GetPerformanceSummaryAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            var data = _performanceData.Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime);
            return Task.FromResult(new PerformanceDataSummary
            {
                TotalRecords = data.Count(),
                AverageValue = data.Any() ? data.Average(p => p.Value) : 0.0,
                MinValue = data.Any() ? data.Min(p => p.Value) : 0.0,
                MaxValue = data.Any() ? data.Max(p => p.Value) : 0.0,
                StartTime = startTime,
                EndTime = endTime
            });
        }
    }
    
}

/// <summary>
/// In-memory implementation of feedback store
/// </summary>
public class InMemoryFeedbackStore : Nexo.Core.Application.Services.Learning.IFeedbackStore
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
            return Task.FromResult(_feedback.FirstOrDefault(f => f.Id == feedbackId));
        }
    }
    
    public Task DeleteFeedbackAsync(string feedbackId)
    {
        lock (_lock)
        {
            _feedback.RemoveAll(f => f.Id == feedbackId);
        }
        return Task.CompletedTask;
    }
    
    public Task<UserFeedback?> GetFeedbackAsync(string id)
    {
        return GetFeedbackByIdAsync(id);
    }
    
    public Task<IEnumerable<UserFeedback>> GetFeedbackAsync(DateTime startTime, DateTime endTime)
    {
        return GetFeedbackInTimeRangeAsync(startTime, endTime);
    }
    
    public Task DeleteOldFeedbackAsync(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _feedback.RemoveAll(f => f.Timestamp < cutoffTime);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory implementation of environment data store
/// </summary>
public class InMemoryEnvironmentDataStore : Nexo.Core.Application.Services.Environment.IEnvironmentDataStore
{
    private Nexo.Core.Application.Services.Environment.DetectedEnvironment? _lastEnvironment;
    private readonly List<Nexo.Core.Application.Services.Environment.EnvironmentChange> _changes = new();
    private readonly object _lock = new();
    
    public Task StoreEnvironmentAsync(Nexo.Core.Application.Services.Environment.DetectedEnvironment environment)
    {
        lock (_lock)
        {
            _lastEnvironment = environment;
        }
        return Task.CompletedTask;
    }
    
    public Task<Nexo.Core.Application.Services.Environment.DetectedEnvironment?> GetLastDetectedEnvironmentAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_lastEnvironment);
        }
    }
    
    public Task RecordEnvironmentChangeAsync(Nexo.Core.Application.Services.Environment.EnvironmentChange change)
    {
        lock (_lock)
        {
            _changes.Add(change);
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<Nexo.Core.Application.Services.Environment.EnvironmentChange>> GetEnvironmentChangeHistoryAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lock)
        {
            return Task.FromResult(_changes.Where(c => c.ChangedAt >= cutoff));
        }
    }
    
    public Task StoreEnvironmentDataAsync(EnvironmentProfile profile)
    {
        lock (_lock)
        {
            _lastEnvironment = new Nexo.Core.Application.Services.Environment.DetectedEnvironment
            {
                Context = new Nexo.Core.Domain.Entities.Infrastructure.EnvironmentContext { Type = profile.Context },
                Platform = profile.Platform,
                Resources = new EnvironmentResources
                {
                    CpuCores = profile.CpuCores,
                    TotalMemoryMB = profile.AvailableMemoryMB
                }
            };
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<EnvironmentProfile>> GetEnvironmentDataAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            var profiles = new List<EnvironmentProfile>();
            if (_lastEnvironment != null && _lastEnvironment.DetectedAt >= startTime && _lastEnvironment.DetectedAt <= endTime)
            {
                profiles.Add(new EnvironmentProfile
                {
                    PlatformType = _lastEnvironment.Platform,
                    Context = _lastEnvironment.Context.Type,
                    Platform = _lastEnvironment.Platform,
                    CpuCores = _lastEnvironment.Resources.CpuCores,
                    AvailableMemoryMB = _lastEnvironment.Resources.TotalMemoryMB
                });
            }
            return Task.FromResult<IEnumerable<EnvironmentProfile>>(profiles);
        }
    }
    
    public Task<EnvironmentProfile?> GetLatestEnvironmentDataAsync()
    {
        lock (_lock)
        {
            if (_lastEnvironment == null) return Task.FromResult<EnvironmentProfile?>(null);
            
            return Task.FromResult<EnvironmentProfile?>(new EnvironmentProfile
            {
                PlatformType = _lastEnvironment.Platform,
                Context = _lastEnvironment.Context.Type,
                Platform = _lastEnvironment.Platform,
                CpuCores = _lastEnvironment.Resources.CpuCores,
                AvailableMemoryMB = _lastEnvironment.Resources.TotalMemoryMB
            });
        }
    }
    
    public Task StoreEnvironmentChangeAsync(Nexo.Core.Application.Services.Environment.EnvironmentChange change)
    {
        return RecordEnvironmentChangeAsync(change);
    }
    
    public Task<IEnumerable<Nexo.Core.Application.Services.Environment.EnvironmentChange>> GetEnvironmentChangesAsync(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return Task.FromResult(_changes.Where(c => c.ChangedAt >= startTime && c.ChangedAt <= endTime));
        }
    }
    
    public Task DeleteOldEnvironmentDataAsync(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _changes.RemoveAll(c => c.ChangedAt < cutoffTime);
            if (_lastEnvironment != null && _lastEnvironment.DetectedAt < cutoffTime)
            {
                _lastEnvironment = null;
            }
        }
        return Task.CompletedTask;
    }
}

public record PerformanceDataSummary
{
    public int TotalRecords { get; init; }
    public double AverageValue { get; init; }
    public double MinValue { get; init; }
    public double MaxValue { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
