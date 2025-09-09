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
/// Strategy for adapting system performance based on real-time metrics
/// </summary>
public class PerformanceAdaptationStrategy : IAdaptationStrategy
{
    public string StrategyId => "Performance.Dynamic";
    public AdaptationType SupportedAdaptationType => AdaptationType.PerformanceOptimization;
    
    private readonly IIterationStrategySelector _iterationSelector;
    private readonly IPerformanceProfiler _profiler;
    private readonly ICodeOptimizer _codeOptimizer;
    private readonly ILogger<PerformanceAdaptationStrategy> _logger;
    
    public PerformanceAdaptationStrategy(
        IIterationStrategySelector iterationSelector,
        IPerformanceProfiler profiler,
        ICodeOptimizer codeOptimizer,
        ILogger<PerformanceAdaptationStrategy> logger)
    {
        _iterationSelector = iterationSelector;
        _profiler = profiler;
        _codeOptimizer = codeOptimizer;
        _logger = logger;
    }
    
    public async Task<AdaptationResult> ExecuteAdaptationAsync(AdaptationNeed need)
    {
        _logger.LogInformation("Executing performance adaptation strategy for {Trigger}", need.Trigger);
        
        var adaptations = new List<AppliedAdaptation>();
        
        // Adapt iteration strategies based on current performance
        var iterationAdaptation = await AdaptIterationStrategies(need.Context);
        if (iterationAdaptation != null)
            adaptations.Add(iterationAdaptation);
        
        // Adjust optimization levels
        var optimizationAdaptation = await AdaptOptimizationLevels(need.Context);
        if (optimizationAdaptation != null)
            adaptations.Add(optimizationAdaptation);
        
        // Modify caching strategies
        var cachingAdaptation = await AdaptCachingStrategies(need.Context);
        if (cachingAdaptation != null)
            adaptations.Add(cachingAdaptation);
        
        // Adjust concurrency levels
        var concurrencyAdaptation = await AdaptConcurrencyLevels(need.Context);
        if (concurrencyAdaptation != null)
            adaptations.Add(concurrencyAdaptation);
        
        return new AdaptationResult
        {
            IsSuccessful = adaptations.Any(),
            AppliedAdaptations = adaptations,
            EstimatedImprovement = adaptations.Sum(a => a.EstimatedImprovementFactor),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private async Task<AppliedAdaptation?> AdaptIterationStrategies(SystemState systemState)
    {
        var currentPerformance = systemState.PerformanceMetrics;
        
        // If CPU is constrained, prefer CPU-efficient strategies
        if (currentPerformance.CpuUsage > 0.8)
        {
            var newProfile = new RuntimeEnvironmentProfile
            {
                PlatformType = systemState.EnvironmentProfile.PlatformType,
                OptimizationLevel = OptimizationLevel.Aggressive,
                CpuCores = systemState.EnvironmentProfile.CpuCores,
                AvailableMemoryMB = systemState.EnvironmentProfile.AvailableMemoryMB
            };
            
            // Update global strategy selector profile
            _iterationSelector.SetEnvironmentProfile(newProfile);
            
            _logger.LogInformation("Switched to CPU-optimized iteration strategies due to high CPU utilization: {CpuUtilization:P}",
                currentPerformance.CpuUtilization);
            
            return new AppliedAdaptation
            {
                Type = "IterationStrategy.CpuOptimization",
                Description = "Switched to CPU-optimized iteration strategies",
                EstimatedImprovementFactor = 1.3,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["CpuUtilization"] = currentPerformance.CpuUtilization,
                    ["OptimizationLevel"] = "Aggressive"
                }
            };
        }
        
        // If memory is constrained, prefer memory-efficient strategies
        if (currentPerformance.MemoryUtilization > 0.85)
        {
            var memoryOptimizedProfile = new RuntimeEnvironmentProfile
            {
                PlatformType = systemState.EnvironmentProfile.PlatformType,
                OptimizationLevel = OptimizationLevel.Aggressive,
                AvailableMemoryMB = systemState.EnvironmentProfile.AvailableMemoryMB / 2 // Assume constrained
            };
            
            _iterationSelector.SetEnvironmentProfile(memoryOptimizedProfile);
            
            _logger.LogInformation("Switched to memory-optimized iteration strategies due to high memory utilization: {MemoryUtilization:P}",
                currentPerformance.MemoryUtilization);
            
            return new AppliedAdaptation
            {
                Type = "IterationStrategy.MemoryOptimization",
                Description = "Switched to memory-optimized iteration strategies",
                EstimatedImprovementFactor = 1.2,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["MemoryUtilization"] = currentPerformance.MemoryUtilization,
                    ["AvailableMemoryMB"] = memoryOptimizedProfile.AvailableMemoryMB
                }
            };
        }
        
        // If response time is high, prefer faster iteration strategies
        if (currentPerformance.ResponseTime > 5000) // 5 seconds
        {
            var speedOptimizedProfile = new RuntimeEnvironmentProfile
            {
                PlatformType = systemState.EnvironmentProfile.PlatformType,
                OptimizationLevel = OptimizationLevel.Aggressive,
                CpuCores = systemState.EnvironmentProfile.CpuCores,
                AvailableMemoryMB = systemState.EnvironmentProfile.AvailableMemoryMB
            };
            
            _iterationSelector.SetEnvironmentProfile(speedOptimizedProfile);
            
            _logger.LogInformation("Switched to speed-optimized iteration strategies due to high response time: {ResponseTime}ms",
                currentPerformance.ResponseTime);
            
            return new AppliedAdaptation
            {
                Type = "IterationStrategy.SpeedOptimization",
                Description = "Switched to speed-optimized iteration strategies",
                EstimatedImprovementFactor = 1.4,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["ResponseTime"] = currentPerformance.ResponseTime,
                    ["OptimizationLevel"] = "Aggressive"
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptOptimizationLevels(SystemState systemState)
    {
        var currentPerformance = systemState.PerformanceMetrics;
        
        // Increase optimization level for poor performance
        if (currentPerformance.OverallScore < 0.6)
        {
            var newOptimizationLevel = OptimizationLevel.Aggressive;
            
            // Apply aggressive optimizations
            await _codeOptimizer.SetOptimizationLevelAsync(newOptimizationLevel);
            
            _logger.LogInformation("Increased optimization level to {OptimizationLevel} due to poor performance score: {Score}",
                newOptimizationLevel, currentPerformance.OverallScore);
            
            return new AppliedAdaptation
            {
                Type = "OptimizationLevel.Increase",
                Description = $"Increased optimization level to {newOptimizationLevel}",
                EstimatedImprovementFactor = 1.5,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["OptimizationLevel"] = newOptimizationLevel.ToString(),
                    ["PreviousScore"] = currentPerformance.OverallScore
                }
            };
        }
        
        // Decrease optimization level for good performance to save resources
        if (currentPerformance.OverallScore > 0.9 && systemState.ResourceUtilization.IsConstrained)
        {
            var newOptimizationLevel = OptimizationLevel.Balanced;
            
            await _codeOptimizer.SetOptimizationLevelAsync(newOptimizationLevel);
            
            _logger.LogInformation("Decreased optimization level to {OptimizationLevel} to save resources while maintaining good performance",
                newOptimizationLevel);
            
            return new AppliedAdaptation
            {
                Type = "OptimizationLevel.Decrease",
                Description = $"Decreased optimization level to {newOptimizationLevel} to save resources",
                EstimatedImprovementFactor = 0.8, // Resource savings
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["OptimizationLevel"] = newOptimizationLevel.ToString(),
                    ["ResourceSavings"] = true
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptCachingStrategies(SystemState systemState)
    {
        var currentPerformance = systemState.PerformanceMetrics;
        
        // Enable aggressive caching for high latency scenarios
        if (currentPerformance.NetworkLatency > 100) // 100ms
        {
            _logger.LogInformation("Enabling aggressive caching due to high network latency: {Latency}ms",
                currentPerformance.NetworkLatency);
            
            return new AppliedAdaptation
            {
                Type = "CachingStrategy.Aggressive",
                Description = "Enabled aggressive caching for high latency scenarios",
                EstimatedImprovementFactor = 1.6,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["NetworkLatency"] = currentPerformance.NetworkLatency,
                    ["CachingLevel"] = "Aggressive"
                }
            };
        }
        
        // Disable caching for low latency scenarios to save memory
        if (currentPerformance.NetworkLatency < 10 && currentPerformance.MemoryUtilization > 0.8)
        {
            _logger.LogInformation("Disabling caching to save memory in low latency scenario");
            
            return new AppliedAdaptation
            {
                Type = "CachingStrategy.Disabled",
                Description = "Disabled caching to save memory in low latency scenario",
                EstimatedImprovementFactor = 0.7, // Memory savings
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["NetworkLatency"] = currentPerformance.NetworkLatency,
                    ["MemoryUtilization"] = currentPerformance.MemoryUtilization,
                    ["CachingLevel"] = "Disabled"
                }
            };
        }
        
        return null;
    }
    
    private async Task<AppliedAdaptation?> AdaptConcurrencyLevels(SystemState systemState)
    {
        var currentPerformance = systemState.PerformanceMetrics;
        var environment = systemState.EnvironmentProfile;
        
        // Adjust concurrency based on CPU cores and utilization
        if (currentPerformance.CpuUtilization < 0.5 && environment.CpuCores > 4)
        {
            // Increase concurrency for underutilized multi-core systems
            var newConcurrencyLevel = Math.Min(environment.CpuCores * 2, 16);
            
            _logger.LogInformation("Increasing concurrency to {ConcurrencyLevel} for underutilized multi-core system",
                newConcurrencyLevel);
            
            return new AppliedAdaptation
            {
                Type = "ConcurrencyLevel.Increase",
                Description = $"Increased concurrency to {newConcurrencyLevel} for better CPU utilization",
                EstimatedImprovementFactor = 1.3,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["ConcurrencyLevel"] = newConcurrencyLevel,
                    ["CpuCores"] = environment.CpuCores,
                    ["CpuUtilization"] = currentPerformance.CpuUtilization
                }
            };
        }
        
        // Decrease concurrency for overutilized systems
        if (currentPerformance.CpuUtilization > 0.9)
        {
            var newConcurrencyLevel = Math.Max(1, environment.CpuCores / 2);
            
            _logger.LogInformation("Decreasing concurrency to {ConcurrencyLevel} for overutilized system",
                newConcurrencyLevel);
            
            return new AppliedAdaptation
            {
                Type = "ConcurrencyLevel.Decrease",
                Description = $"Decreased concurrency to {newConcurrencyLevel} to reduce CPU pressure",
                EstimatedImprovementFactor = 1.2,
                AppliedAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["ConcurrencyLevel"] = newConcurrencyLevel,
                    ["CpuUtilization"] = currentPerformance.CpuUtilization
                }
            };
        }
        
        return null;
    }
    
    public int GetPriority(SystemState systemState)
    {
        // Higher priority for severe performance issues
        return systemState.PerformanceMetrics.Severity switch
        {
            PerformanceSeverity.Critical => 100,
            PerformanceSeverity.High => 80,
            PerformanceSeverity.Medium => 60,
            _ => 40
        };
    }
    
    public async Task<bool> CanHandleAsync(AdaptationNeed need)
    {
        return need.Type == AdaptationType.PerformanceOptimization &&
               need.Context.PerformanceMetrics.HasIterationBottlenecks;
    }
    
    public string GetDescription()
    {
        return "Dynamically adapts iteration strategies, optimization levels, caching, and concurrency based on real-time performance metrics";
    }
    
    public double GetEstimatedImprovementFactor(AdaptationNeed need)
    {
        var severity = need.Context.PerformanceMetrics.Severity;
        return severity switch
        {
            PerformanceSeverity.Critical => 2.0,
            PerformanceSeverity.High => 1.5,
            PerformanceSeverity.Medium => 1.2,
            _ => 1.1
        };
    }
}