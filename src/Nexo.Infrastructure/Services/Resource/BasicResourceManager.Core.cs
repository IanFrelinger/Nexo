using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Infrastructure.Services.Resource
{
    /// <summary>
    /// Core resource management operations
    /// </summary>
    public partial class BasicResourceManager
    {
        public async Task<ResourceUsage> GetUsageAsync(CancellationToken cancellationToken = default)
        {
            var usage = new ResourceUsage
            {
                Timestamp = DateTime.UtcNow,
                ActiveAllocations = _allocations.Values.ToList()
            };

            // Calculate usage by type
            foreach (var resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                var resourceTypeEnum = (ResourceType)resourceType;
                var allocations = _allocations.Values.Where(a => a.ResourceType == resourceTypeEnum);
                usage.AllocatedByType[resourceTypeEnum] = allocations.Sum(a => a.Amount);

                // Get available resources from providers
                var available = 0L;
                foreach (var provider in _providers.Values)
                {
                    if (!provider.SupportedResourceTypes.Contains(resourceTypeEnum)) continue;
                    var availability = await provider.GetAvailabilityAsync(cancellationToken);
                    if (availability.AvailableByType.TryGetValue(resourceTypeEnum, out var providerAvailable))
                    {
                        available += providerAvailable;
                    }
                }
                usage.AvailableByType[resourceTypeEnum] = available;

                // Calculate utilization
                var total = usage.AllocatedByType[resourceTypeEnum] + available;
                if (total > 0)
                {
                    usage.UtilizationByType[resourceTypeEnum] = (double)usage.AllocatedByType[resourceTypeEnum] / total * 100;
                }
                else
                {
                    usage.UtilizationByType[resourceTypeEnum] = 0;
                }
            }

            return usage;
        }

        public Task<ResourceLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_limits);
        }

        public async Task<ResourceMonitoringInfo> MonitorAsync(CancellationToken cancellationToken = default)
        {
            var monitoringInfo = new ResourceMonitoringInfo
            {
                Alerts = _alerts.ToList(),
                MetricsByType = _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                HealthStatus = await AssessHealthAsync(cancellationToken)
            };

            return monitoringInfo;
        }

        public void Dispose()
        {
            _monitoringTimer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _allocationLock?.Dispose();
        }
    }
}
