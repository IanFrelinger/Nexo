using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Adaptation
{
    /// <summary>
    /// In-memory implementation of adaptation data store
    /// </summary>
    public partial class InMemoryAdaptationDataStore : IAdaptationDataStore
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
}
