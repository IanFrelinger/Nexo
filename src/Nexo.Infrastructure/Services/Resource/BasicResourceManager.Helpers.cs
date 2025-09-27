using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Infrastructure.Services.Resource
{
    /// <summary>
    /// Helper methods and data models
    /// </summary>
    public partial class BasicResourceManager
    {
        private void InitializeDefaultLimits()
        {
            // Set default limits based on system capabilities
            var processorCount = Environment.ProcessorCount;
            var memoryMb = GC.GetTotalMemory(false) / (1024 * 1024);

            _limits.MaximumByType[ResourceType.CPU] = processorCount * 100; // CPU percentage
            _limits.MaximumByType[ResourceType.Memory] = memoryMb * 1024 * 1024; // Memory in bytes
            _limits.MaximumByType[ResourceType.GPU] = 1; // Default to 1 GPU
            _limits.MaximumByType[ResourceType.Storage] = 100 * 1024 * 1024 * 1024L; // 100GB default
            _limits.MaximumByType[ResourceType.Network] = 100 * 1024 * 1024; // 100MB/s default
            _limits.MaximumByType[ResourceType.AIModel] = 1; // Default to 1 AI model

            // Set soft limits at 80% of maximum
            foreach (var kvp in _limits.MaximumByType)
            {
                _limits.SoftLimitsByType[kvp.Key] = (long)(kvp.Value * 0.8);
            }

            // Set hard limits at 95% of maximum
            foreach (var kvp in _limits.MaximumByType)
            {
                _limits.HardLimitsByType[kvp.Key] = (long)(kvp.Value * 0.95);
            }

            // Set default policies
            foreach (var resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                _limits.PoliciesByType[(ResourceType)resourceType] = new ResourceAllocationPolicy
                {
                    MaxAllocationPerRequest = _limits.MaximumByType[(ResourceType)resourceType] / 4,
                    MinAllocationPerRequest = 1,
                    AllocationTimeout = TimeSpan.FromMinutes(5),
                    AllowOverAllocation = false,
                    OverAllocationLimitPercentage = 10
                };
            }
        }

        private async Task<IResourceProvider?> FindSuitableProviderAsync(ResourceAllocationRequest request, CancellationToken cancellationToken)
        {
            var suitableProviders = _providers.Values
                .Where(p => p.SupportedResourceTypes.Contains(request.ResourceType))
                .ToList();

            if (!suitableProviders.Any())
                return null;

            // Check availability for each provider
            var availableProviders = new List<(IResourceProvider Provider, ResourceAvailability Availability)>();
            foreach (var provider in suitableProviders)
            {
                try
                {
                    var availability = await provider.GetAvailabilityAsync(cancellationToken);
                    if (availability.IsHealthy && availability.AvailableByType.TryGetValue(request.ResourceType, out var available) && available >= request.Amount)
                    {
                        availableProviders.Add((provider, availability));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check availability for provider {ProviderId}", provider.ProviderId);
                }
            }

            if (!availableProviders.Any())
                return null;

            // Select the provider with the most available resources
            return availableProviders
                .OrderByDescending(x => x.Availability.AvailableByType[request.ResourceType])
                .First().Provider;
        }

        private async Task<bool> CheckLimitsAsync(ResourceAllocationRequest request, CancellationToken cancellationToken)
        {
            var usage = await GetUsageAsync(cancellationToken);
            var currentAllocated = usage.AllocatedByType.TryGetValue(request.ResourceType, out var currentAllocatedAmount) ? currentAllocatedAmount : 0;
            var newTotal = currentAllocated + request.Amount;

            if (_limits.HardLimitsByType.TryGetValue(request.ResourceType, out var hardLimit))
            {
                if (newTotal > hardLimit)
                {
                    _logger.LogWarning("Allocation would exceed hard limit for {ResourceType}: {Requested} > {Limit}", 
                        request.ResourceType, newTotal, hardLimit);
                    return false;
                }
            }

            if (!_limits.SoftLimitsByType.TryGetValue(request.ResourceType, out var softLimit)) return true;
            if (newTotal > softLimit)
            {
                _logger.LogWarning("Allocation would exceed soft limit for {ResourceType}: {Requested} > {Limit}", 
                    request.ResourceType, newTotal, softLimit);
                // Allow but log warning
            }

            return true;
        }

        private void UpdateMetrics(ResourceType resourceType, bool isAllocation)
        {
            if (!_metrics.TryGetValue(resourceType, out ResourceMetrics? value))
            {
                value = new ResourceMetrics();
                _metrics[resourceType] = value;
            }

            var metrics = value;
            if (isAllocation)
            {
                metrics.AllocationCount++;
            }
            else
            {
                metrics.ReleaseCount++;
            }
        }
    }
}
