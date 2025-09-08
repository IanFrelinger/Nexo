using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces.Resource;
using System.Linq;

namespace Nexo.Infrastructure.Services.Resource
{
/// <summary>
/// Basic resource manager implementation with allocation tracking and monitoring.
/// </summary>
public class BasicResourceManager : IResourceManager
{
    private readonly ILogger<BasicResourceManager> _logger;
    private readonly ConcurrentDictionary<string, IResourceProvider> _providers = new ConcurrentDictionary<string, IResourceProvider>();
    private readonly ConcurrentDictionary<string, ResourceAllocation> _allocations = new ConcurrentDictionary<string, ResourceAllocation>();
    private readonly SemaphoreSlim _allocationLock = new(1, 1);
    private readonly Timer _monitoringTimer;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;

    private readonly ResourceLimits _limits = new();
    private readonly List<ResourceAlert> _alerts = [];
    private readonly Dictionary<ResourceType, ResourceMetrics> _metrics = new();

    public BasicResourceManager(ILogger<BasicResourceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize default limits
        InitializeDefaultLimits();

        // Initialize performance counters (if available)
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _cpuCounter = new PerformanceCounter($"Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _logger.LogInformation("Performance counters initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize performance counters, using fallback monitoring");
        }

        // Start monitoring timer
        _monitoringTimer = new Timer(MonitorResources, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        _logger.LogInformation("Basic resource manager initialized");
    }

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

    public async Task<ResourceLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_limits);
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

    private async Task<ResourceHealthStatus> AssessHealthAsync(CancellationToken cancellationToken)
    {
        var healthStatus = new ResourceHealthStatus
        {
            LastCheckTime = DateTime.UtcNow
        };

        var usage = await GetUsageAsync(cancellationToken);
        var overallHealth = ResourceHealth.Healthy;

        foreach (var kvp in usage.UtilizationByType)
        {
            var resourceType = kvp.Key;
            var utilization = kvp.Value;

            switch (utilization)
            {
                case > 95:
                    healthStatus.StatusByType[resourceType] = ResourceHealth.Unhealthy;
                    overallHealth = ResourceHealth.Unhealthy;
                    break;
                case > 80:
                {
                    healthStatus.StatusByType[resourceType] = ResourceHealth.Degraded;
                    if (overallHealth == ResourceHealth.Healthy)
                    {
                        overallHealth = ResourceHealth.Degraded;
                    }

                    break;
                }
                default:
                    healthStatus.StatusByType[resourceType] = ResourceHealth.Healthy;
                    break;
            }
        }

        healthStatus.OverallStatus = overallHealth;
        return healthStatus;
    }

    private void MonitorResources(object? state)
    {
        try
        {
            // Update CPU and memory metrics
            if (_cpuCounter != null && OperatingSystem.IsWindows())
            {
                var cpuUsage = _cpuCounter.NextValue();
                if (!_metrics.ContainsKey(ResourceType.CPU))
                    _metrics[ResourceType.CPU] = new ResourceMetrics();

                _metrics[ResourceType.CPU].AverageUtilization = cpuUsage;
                _metrics[ResourceType.CPU].PeakUtilization = Math.Max(_metrics[ResourceType.CPU].PeakUtilization, cpuUsage);
            }

            if (_memoryCounter is not null && OperatingSystem.IsWindows())
            {
                var availableMemory = _memoryCounter.NextValue();
                // Convert to utilization percentage (assuming total memory from limits)
                if (_limits.MaximumByType.TryGetValue(ResourceType.Memory, out var totalMemory))
                {
                    var totalMemoryMb = totalMemory / (1024 * 1024);
                    var memoryUtilization = ((totalMemoryMb - availableMemory) / totalMemoryMb) * 100;

                    if (!_metrics.ContainsKey(ResourceType.Memory))
                        _metrics[ResourceType.Memory] = new ResourceMetrics();

                    _metrics[ResourceType.Memory].AverageUtilization = memoryUtilization;
                    _metrics[ResourceType.Memory].PeakUtilization = Math.Max(_metrics[ResourceType.Memory].PeakUtilization, memoryUtilization);
                }
            }

            // Check for alerts
            CheckForAlerts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource monitoring");
        }
    }

    private void CheckForAlerts()
    {
        // Clear old alerts
        _alerts.RemoveAll(a => a.Timestamp < DateTime.UtcNow.AddMinutes(-5));

        // Check for high utilization
        foreach (var alert in from kvp in _metrics let resourceType = kvp.Key let metrics = kvp.Value where metrics.AverageUtilization > 90 select new ResourceAlert
                 {
                     AlertId = Guid.NewGuid().ToString(),
                     Type = ResourceAlertType.HighUtilization,
                     Severity = ResourceAlertSeverity.Warning,
                     Message = $"High {resourceType} utilization: {metrics.AverageUtilization:F1}%",
                     ResourceType = resourceType,
                     Timestamp = DateTime.UtcNow
                 } into alert where !_alerts.Any(a => a.Type == alert.Type && a.ResourceType == alert.ResourceType) select alert)
        {
            _alerts.Add(alert);
            _logger.LogWarning("Resource alert: {Message}", alert.Message);
        }
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