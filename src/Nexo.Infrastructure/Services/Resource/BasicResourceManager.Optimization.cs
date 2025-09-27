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
    /// Optimization and provider management functionality
    /// </summary>
    public partial class BasicResourceManager
    {
        public async Task<ResourceOptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default)
        {
            var result = new ResourceOptimizationResult { IsSuccessful = true };
            var usage = await GetUsageAsync(cancellationToken);

            // Generate optimization recommendations
            foreach (var kvp in usage.UtilizationByType)
            {
                var resourceType = kvp.Key;
                var utilization = kvp.Value;

                switch (utilization)
                {
                    case > 90:
                        result.Recommendations.Add(new ResourceOptimizationRecommendation
                        {
                            Type = "HighUtilization",
                            Message = $"High utilization ({utilization:F1}%) detected for {resourceType}",
                            Impact = "Consider scaling up or redistributing load",
                            Priority = 1
                        });
                        break;
                    case < 20:
                        result.Recommendations.Add(new ResourceOptimizationRecommendation
                        {
                            Type = "LowUtilization",
                            Message = $"Low utilization ({utilization:F1}%) detected for {resourceType}",
                            Impact = "Consider scaling down to reduce costs",
                            Priority = 3
                        });
                        break;
                }
            }

            // Check for expired allocations
            var expiredAllocations = _allocations.Values.Where(a => a.ExpiresAt <= DateTime.UtcNow);
            var resourceAllocations = expiredAllocations as ResourceAllocation[] ?? expiredAllocations.ToArray();
            if (resourceAllocations.Any())
            {
                result.Recommendations.Add(new ResourceOptimizationRecommendation
                {
                    Type = "ExpiredAllocations",
                    Message = $"{resourceAllocations.Count()} expired allocations found",
                    Impact = "Release expired allocations to free up resources",
                    Priority = 2
                });
            }

            _logger.LogDebug("Generated {Count} optimization recommendations", result.Recommendations.Count);
            return result;
        }

        public async Task RegisterProviderAsync(IResourceProvider provider, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(provider);

            _providers[provider.ProviderId] = provider;
            _logger.LogInformation("Registered resource provider: {ProviderName} ({ProviderId})", 
                provider.Name, provider.ProviderId);

            await Task.CompletedTask;
        }

        public async Task UnregisterProviderAsync(string providerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("ProviderId cannot be null or empty", nameof(providerId));

            if (_providers.TryRemove(providerId, out var provider))
            {
                _logger.LogInformation("Unregistered resource provider: {ProviderName} ({ProviderId})", 
                    provider.Name, providerId);
            }

            await Task.CompletedTask;
        }
    }
}
