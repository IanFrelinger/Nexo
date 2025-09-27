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
    /// Allocation and release operations
    /// </summary>
    public partial class BasicResourceManager
    {
        public async Task<ResourceAllocationResult> AllocateAsync(ResourceAllocationRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            await _allocationLock.WaitAsync(cancellationToken);
            try
            {
                _logger.LogDebug("Allocating {Amount} of {ResourceType} for {RequesterId}", 
                    request.Amount, request.ResourceType, request.RequesterId);

                // Find suitable provider
                var provider = await FindSuitableProviderAsync(request, cancellationToken);
                if (provider == null)
                {
                    var errorMessage = $"No suitable provider found for {request.ResourceType}";
                    _logger.LogWarning(errorMessage);
                    return new ResourceAllocationResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = errorMessage
                    };
                }

                // Check limits
                if (!await CheckLimitsAsync(request, cancellationToken))
                {
                    var errorMessage = $"Resource allocation would exceed limits for {request.ResourceType}";
                    _logger.LogWarning(errorMessage);
                    return new ResourceAllocationResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = errorMessage
                    };
                }

                // Allocate from provider
                var result = await provider.AllocateAsync(request, cancellationToken);
                if (!result.IsSuccessful) return result;
                // Track allocation
                var allocation = new ResourceAllocation
                {
                    AllocationId = result.AllocationId,
                    ResourceType = request.ResourceType,
                    Amount = result.AllocatedAmount,
                    RequesterId = request.RequesterId,
                    AllocatedAt = DateTime.UtcNow,
                    ExpiresAt = result.ExpiresAt,
                    Priority = request.Priority
                };

                _allocations[result.AllocationId] = allocation;
                UpdateMetrics(request.ResourceType, true);

                _logger.LogInformation("Successfully allocated {Amount} of {ResourceType} for {RequesterId}", 
                    result.AllocatedAmount, request.ResourceType, request.RequesterId);

                return result;
            }
            finally
            {
                _allocationLock.Release();
            }
        }

        public async Task ReleaseAsync(string allocationId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(allocationId))
                throw new ArgumentException("AllocationId cannot be null or empty", nameof(allocationId));

            await _allocationLock.WaitAsync(cancellationToken);
            try
            {
                if (_allocations.TryRemove(allocationId, out var allocation))
                {
                    // Find provider and release - we need to track which provider was used
                    // For now, we'll try all providers since we don't track which one was used
                    foreach (var provider in _providers.Values)
                    {
                        try
                        {
                            await provider.ReleaseAsync(allocationId, cancellationToken);
                            break; // Stop after first successful release
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Provider {ProviderId} could not release allocation {AllocationId}", 
                                provider.ProviderId, allocationId);
                        }
                    }

                    UpdateMetrics(allocation.ResourceType, false);
                    _logger.LogInformation("Released allocation {AllocationId} for {ResourceType}", 
                        allocationId, allocation.ResourceType);
                }
                else
                {
                    _logger.LogWarning("Attempted to release non-existent allocation: {AllocationId}", allocationId);
                }
            }
            finally
            {
                _allocationLock.Release();
            }
        }
    }
}
