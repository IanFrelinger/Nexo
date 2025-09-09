using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation.Strategies;

/// <summary>
/// Strategy for adapting system behavior based on resource constraints
/// </summary>
public class ResourceAdaptationStrategy : IAdaptationStrategy
{
    public string StrategyId => "Resource.Dynamic";
    public AdaptationType SupportedAdaptationType => AdaptationType.ResourceOptimization;
    
    private readonly IResourceManager _resourceManager;
    private readonly ILogger<ResourceAdaptationStrategy> _logger;
    
    public ResourceAdaptationStrategy(
        IResourceManager resourceManager,
        ILogger<ResourceAdaptationStrategy> logger)
    {
        _resourceManager = resourceManager;
        _logger = logger;
    }
    
    public async Task<AdaptationResult> ExecuteAdaptationAsync(AdaptationNeed need)
    {
        _logger.LogInformation("Executing resource adaptation strategy for constraint: {ConstraintType}", 
            need.Context.ResourceUtilization.ConstraintType);
        
        var adaptations = new List<AppliedAdaptation>();
        
        // Adapt based on specific resource constraints
        switch (need.Context.ResourceUtilization.ConstraintType)
        {
            case ResourceConstraintType.Cpu:
                var cpuAdaptation = await AdaptForCpuConstraint(need.Context);
                if (cpuAdaptation != null) adaptations.Add(cpuAdaptation);
                break;
                
            case ResourceConstraintType.Memory:
                var memoryAdaptation = await AdaptForMemoryConstraint(need.Context);
                if (memoryAdaptation != null) adaptations.Add(memoryAdaptation);
                break;
                
            case ResourceConstraintType.Disk:
                var diskAdaptation = await AdaptForDiskConstraint(need.Context);
                if (diskAdaptation != null) adaptations.Add(diskAdaptation);
                break;
                
            case ResourceConstraintType.Network:
                var networkAdaptation = await AdaptForNetworkConstraint(need.Context);
                if (networkAdaptation != null) adaptations.Add(networkAdaptation);
                break;
        }
        
        return new AdaptationResult
        {
            IsSuccessful = adaptations.Any(),
            AppliedAdaptations = adaptations,
            EstimatedImprovement = adaptations.Sum(a => a.EstimatedImprovementFactor),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private async Task<AppliedAdaptation?> AdaptForCpuConstraint(SystemState systemState)
    {
        var cpuUsage = systemState.ResourceUtilization.CpuUsage;
        
        if (cpuUsage > 0.9)
        {
            // Reduce CPU-intensive operations
            await _resourceManager.SetCpuIntensiveOperationsLimit(0.5);
            
            _logger.LogInformation("Reduced CPU-intensive operations limit to 50% due to high CPU usage: {CpuUsage:P}",
                cpuUsage);
            
            return new AppliedAdaptation
            {
                Type = "Resource.CpuLimit",
                Description = "Reduced CPU-intensive operations limit to 50%",
                EstimatedImprovementFactor = 1.4,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["CpuUsage"] = cpuUsage,
                    ["CpuIntensiveLimit"] = 0.5
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptForMemoryConstraint(SystemState systemState)
    {
        var memoryUsage = systemState.ResourceUtilization.MemoryUsage;
        
        if (memoryUsage > 0.85)
        {
            // Enable aggressive garbage collection
            await _resourceManager.EnableAggressiveGarbageCollection();
            
            // Reduce memory cache sizes
            await _resourceManager.SetMemoryCacheLimit(0.3);
            
            _logger.LogInformation("Enabled aggressive garbage collection and reduced memory cache due to high memory usage: {MemoryUsage:P}",
                memoryUsage);
            
            return new AppliedAdaptation
            {
                Type = "Resource.MemoryOptimization",
                Description = "Enabled aggressive garbage collection and reduced memory cache",
                EstimatedImprovementFactor = 1.3,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["MemoryUsage"] = memoryUsage,
                    ["MemoryCacheLimit"] = 0.3,
                    ["AggressiveGC"] = true
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptForDiskConstraint(SystemState systemState)
    {
        var diskUsage = systemState.ResourceUtilization.DiskUsage;
        
        if (diskUsage > 0.9)
        {
            // Clean up temporary files
            await _resourceManager.CleanupTemporaryFiles();
            
            // Reduce disk cache
            await _resourceManager.SetDiskCacheLimit(0.2);
            
            _logger.LogInformation("Cleaned up temporary files and reduced disk cache due to high disk usage: {DiskUsage:P}",
                diskUsage);
            
            return new AppliedAdaptation
            {
                Type = "Resource.DiskOptimization",
                Description = "Cleaned up temporary files and reduced disk cache",
                EstimatedImprovementFactor = 1.2,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["DiskUsage"] = diskUsage,
                    ["DiskCacheLimit"] = 0.2,
                    ["TempFilesCleaned"] = true
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptForNetworkConstraint(SystemState systemState)
    {
        var networkUsage = systemState.ResourceUtilization.NetworkUsage;
        
        if (networkUsage > 0.8)
        {
            // Enable network request batching
            await _resourceManager.EnableNetworkRequestBatching();
            
            // Increase request timeouts
            await _resourceManager.SetNetworkTimeoutMultiplier(2.0);
            
            _logger.LogInformation("Enabled network request batching and increased timeouts due to high network usage: {NetworkUsage:P}",
                networkUsage);
            
            return new AppliedAdaptation
            {
                Type = "Resource.NetworkOptimization",
                Description = "Enabled network request batching and increased timeouts",
                EstimatedImprovementFactor = 1.3,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["NetworkUsage"] = networkUsage,
                    ["RequestBatching"] = true,
                    ["TimeoutMultiplier"] = 2.0
                }
            };
        }
        
        return null;
    }
    
    public int GetPriority(SystemState systemState)
    {
        return systemState.ResourceUtilization.ConstraintType switch
        {
            ResourceConstraintType.Cpu => 90,
            ResourceConstraintType.Memory => 85,
            ResourceConstraintType.Network => 80,
            ResourceConstraintType.Disk => 75,
            _ => 50
        };
    }
    
    public async Task<bool> CanHandleAsync(AdaptationNeed need)
    {
        return need.Type == AdaptationType.ResourceOptimization &&
               need.Context.ResourceUtilization.IsConstrained;
    }
    
    public string GetDescription()
    {
        return "Adapts system behavior based on resource constraints (CPU, memory, disk, network)";
    }
    
    public double GetEstimatedImprovementFactor(AdaptationNeed need)
    {
        var constraintType = need.Context.ResourceUtilization.ConstraintType;
        return constraintType switch
        {
            ResourceConstraintType.Cpu => 1.4,
            ResourceConstraintType.Memory => 1.3,
            ResourceConstraintType.Network => 1.3,
            ResourceConstraintType.Disk => 1.2,
            _ => 1.1
        };
    }
}

/// <summary>
/// Interface for resource management operations
/// </summary>
public interface IResourceManager
{
    Task SetCpuIntensiveOperationsLimit(double limit);
    Task EnableAggressiveGarbageCollection();
    Task SetMemoryCacheLimit(double limit);
    Task CleanupTemporaryFiles();
    Task SetDiskCacheLimit(double limit);
    Task EnableNetworkRequestBatching();
    Task SetNetworkTimeoutMultiplier(double multiplier);
}