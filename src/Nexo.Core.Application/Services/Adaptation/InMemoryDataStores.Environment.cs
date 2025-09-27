using Nexo.Core.Application.Services.Environment;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Adaptation
{
    /// <summary>
    /// In-memory implementation of environment data store
    /// </summary>
    public partial class InMemoryEnvironmentDataStore : IEnvironmentDataStore
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
        
        public Task StoreEnvironmentDataAsync(EnvironmentProfile profile)
        {
            lock (_lock)
            {
                _lastEnvironment = new DetectedEnvironment
                {
                    Context = new EnvironmentContext { Type = profile.Context },
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
        
        public Task StoreEnvironmentChangeAsync(EnvironmentChange change)
        {
            return RecordEnvironmentChangeAsync(change);
        }
        
        public Task<IEnumerable<EnvironmentChange>> GetEnvironmentChangesAsync(DateTime startTime, DateTime endTime)
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
}
