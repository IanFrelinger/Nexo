using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Nexo iteration strategy selector that chooses optimal strategies based on context
/// </summary>
public class NexoIterationStrategySelector : IIterationStrategySelector
{
    private readonly IEnumerable<IIterationStrategy<object>> _strategies;
    private readonly ILogger<NexoIterationStrategySelector> _logger;
    
    public NexoIterationStrategySelector(
        IEnumerable<IIterationStrategy<object>> strategies,
        ILogger<NexoIterationStrategySelector> logger)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public IIterationStrategy<T> SelectStrategy<T>(IterationContext context)
    {
        try
        {
            _logger.LogDebug("Selecting iteration strategy for context: DataSize={DataSize}, Platform={Platform}", 
                context.DataSize, context.TargetPlatform);
            
            // Get available strategies for this context
            var availableStrategies = GetAvailableStrategies<T>(context).ToList();
            
            if (!availableStrategies.Any())
            {
                _logger.LogWarning("No available strategies for context, using default");
                return GetDefaultStrategy<T>();
            }
            
            // Select strategy with highest priority
            var selectedStrategy = availableStrategies
                .OrderByDescending(s => s.GetPriority(CreatePipelineContext(context)))
                .First();
            
            _logger.LogInformation("Selected iteration strategy {StrategyId} for context", selectedStrategy.StrategyId);
            
            return selectedStrategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting iteration strategy");
            return GetDefaultStrategy<T>();
        }
    }
    
    public IEnumerable<IIterationStrategy<T>> GetAvailableStrategies<T>(IterationContext context)
    {
        var pipelineContext = CreatePipelineContext(context);
        
        return _strategies
            .Where(s => s.CanHandle(pipelineContext))
            .Cast<IIterationStrategy<T>>()
            .OrderByDescending(s => s.GetPriority(pipelineContext));
    }
    
    public string GetSelectionReasoning(IterationContext context)
    {
        var availableStrategies = GetAvailableStrategies<object>(context).ToList();
        
        if (!availableStrategies.Any())
        {
            return "No strategies available for the given context.";
        }
        
        var selectedStrategy = availableStrategies.First();
        var reasoning = new List<string>();
        
        // Add context information
        reasoning.Add($"Data size: {context.DataSize}");
        reasoning.Add($"Target platform: {context.TargetPlatform}");
        reasoning.Add($"CPU cores: {context.EnvironmentProfile.CpuCores}");
        reasoning.Add($"Available memory: {context.EnvironmentProfile.AvailableMemoryMB} MB");
        
        // Add strategy-specific reasoning
        reasoning.Add($"Selected strategy: {selectedStrategy.StrategyId}");
        reasoning.Add($"Performance profile: CPU={selectedStrategy.PerformanceProfile.CpuEfficiency}, Memory={selectedStrategy.PerformanceProfile.MemoryEfficiency}");
        
        // Add why this strategy was chosen
        if (context.Requirements.RequiresRealTime && selectedStrategy.PerformanceProfile.SuitableForRealTime)
        {
            reasoning.Add("Strategy selected for real-time performance requirements.");
        }
        
        if (context.DataSize < 1000 && selectedStrategy.PerformanceProfile.OptimalDataSizeMin <= context.DataSize)
        {
            reasoning.Add("Strategy optimized for small datasets.");
        }
        
        if (context.Requirements.PreferParallel && selectedStrategy.PerformanceProfile.SupportsParallelization)
        {
            reasoning.Add("Strategy selected for parallel processing preference.");
        }
        
        return string.Join("; ", reasoning);
    }
    
    public PerformanceEstimate EstimatePerformance(IIterationStrategy<object> strategy, IterationContext context)
    {
        return strategy.EstimatePerformance(context);
    }
    
    public async Task<IEnumerable<StrategyComparisonResult>> CompareStrategies<T>(IterationContext context)
    {
        var availableStrategies = GetAvailableStrategies<T>(context).ToList();
        var results = new List<StrategyComparisonResult>();
        
        foreach (var strategy in availableStrategies)
        {
            var performanceEstimate = strategy.EstimatePerformance(context);
            var suitabilityScore = CalculateSuitabilityScore(strategy, context, performanceEstimate);
            var reasoning = GenerateStrategyReasoning(strategy, context, performanceEstimate);
            
            results.Add(new StrategyComparisonResult
            {
                Strategy = strategy as IIterationStrategy<object>,
                PerformanceEstimate = performanceEstimate,
                SuitabilityScore = suitabilityScore,
                Reasoning = reasoning,
                IsRecommended = suitabilityScore > 80
            });
        }
        
        return results.OrderByDescending(r => r.SuitabilityScore);
    }
    
    public IEnumerable<StrategyRecommendation> GetRecommendations(PlatformType platform)
    {
        var recommendations = new List<StrategyRecommendation>();
        
        switch (platform)
        {
            case PlatformType.Unity:
                recommendations.AddRange(GetUnityRecommendations());
                break;
            case PlatformType.DotNet:
                recommendations.AddRange(GetDotNetRecommendations());
                break;
            case PlatformType.WebAssembly:
                recommendations.AddRange(GetWebAssemblyRecommendations());
                break;
            case PlatformType.Mobile:
                recommendations.AddRange(GetMobileRecommendations());
                break;
            case PlatformType.Server:
                recommendations.AddRange(GetServerRecommendations());
                break;
            default:
                recommendations.AddRange(GetDefaultRecommendations());
                break;
        }
        
        return recommendations;
    }
    
    private IIterationStrategy<T> GetDefaultStrategy<T>()
    {
        // Return a default strategy (for-loop) if no specific strategy is available
        return new NexoForLoopStrategy<T>() as IIterationStrategy<T>;
    }
    
    private PipelineContext CreatePipelineContext(IterationContext context)
    {
        // Create a pipeline context from iteration context
        var pipelineContext = new PipelineContext();
        pipelineContext.SetProperty("EstimatedDataSize", context.DataSize);
        pipelineContext.SetProperty("TargetPlatform", context.TargetPlatform);
        pipelineContext.SetProperty("PerformanceRequirements", context.Requirements);
        pipelineContext.SetProperty("EnvironmentProfile", context.EnvironmentProfile);
        
        return pipelineContext;
    }
    
    private double CalculateSuitabilityScore(IIterationStrategy<object> strategy, IterationContext context, PerformanceEstimate estimate)
    {
        var score = 0.0;
        
        // Base score from performance estimate
        score += estimate.PerformanceScore * 0.4;
        
        // Platform compatibility score
        if (strategy.PlatformCompatibility.HasFlag(GetPlatformCompatibility(context.TargetPlatform)))
        {
            score += 20;
        }
        
        // Data size suitability
        if (context.DataSize >= strategy.PerformanceProfile.OptimalDataSizeMin &&
            context.DataSize <= strategy.PerformanceProfile.OptimalDataSizeMax)
        {
            score += 15;
        }
        
        // Real-time suitability
        if (context.Requirements.RequiresRealTime && strategy.PerformanceProfile.SuitableForRealTime)
        {
            score += 15;
        }
        else if (context.Requirements.RequiresRealTime && !strategy.PerformanceProfile.SuitableForRealTime)
        {
            score -= 20;
        }
        
        // Parallel processing preference
        if (context.Requirements.PreferParallel && strategy.PerformanceProfile.SupportsParallelization)
        {
            score += 10;
        }
        
        return Math.Max(0, Math.Min(100, score));
    }
    
    private string GenerateStrategyReasoning(IIterationStrategy<object> strategy, IterationContext context, PerformanceEstimate estimate)
    {
        var reasoning = new List<string>();
        
        reasoning.Add($"Strategy: {strategy.StrategyId}");
        reasoning.Add($"Performance Score: {estimate.PerformanceScore:F1}");
        reasoning.Add($"Estimated Time: {estimate.EstimatedExecutionTimeMs:F2}ms");
        reasoning.Add($"Estimated Memory: {estimate.EstimatedMemoryUsageMB:F2}MB");
        
        if (estimate.MeetsRequirements)
        {
            reasoning.Add("Meets all performance requirements");
        }
        else
        {
            reasoning.Add("Does not meet some performance requirements");
        }
        
        return string.Join("; ", reasoning);
    }
    
    private PlatformCompatibility GetPlatformCompatibility(PlatformTarget platform)
    {
        return platform switch
        {
            PlatformTarget.DotNet => PlatformCompatibility.DotNet,
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => PlatformCompatibility.Unity,
            PlatformTarget.WebAssembly => PlatformCompatibility.WebAssembly,
            PlatformTarget.JavaScript => PlatformCompatibility.Browser,
            PlatformTarget.Swift or PlatformTarget.Kotlin => PlatformCompatibility.Mobile,
            PlatformTarget.Server => PlatformCompatibility.Server,
            _ => PlatformCompatibility.All
        };
    }
    
    private IEnumerable<StrategyRecommendation> GetUnityRecommendations()
    {
        return new[]
        {
            new StrategyRecommendation
            {
                Scenario = "Real-time game loop iteration",
                RecommendedStrategyId = "Nexo.UnityOptimized",
                Reasoning = "Optimized for Unity's performance characteristics with minimal allocations",
                DataSizeRange = (0, 10000),
                PerformanceCharacteristics = "Excellent CPU and memory efficiency, suitable for real-time"
            },
            new StrategyRecommendation
            {
                Scenario = "Small dataset processing",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides best performance for small datasets in Unity",
                DataSizeRange = (0, 1000),
                PerformanceCharacteristics = "Excellent performance, minimal overhead"
            }
        };
    }
    
    private IEnumerable<StrategyRecommendation> GetDotNetRecommendations()
    {
        return new[]
        {
            new StrategyRecommendation
            {
                Scenario = "High-performance data processing",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides best performance for most scenarios",
                DataSizeRange = (0, 100000),
                PerformanceCharacteristics = "Excellent CPU and memory efficiency"
            },
            new StrategyRecommendation
            {
                Scenario = "CPU-intensive parallel processing",
                RecommendedStrategyId = "Nexo.ParallelLinq",
                Reasoning = "Parallel LINQ excels at CPU-intensive operations with large datasets",
                DataSizeRange = (10000, int.MaxValue),
                PerformanceCharacteristics = "Excellent scalability with multiple CPU cores"
            },
            new StrategyRecommendation
            {
                Scenario = "Readable functional code",
                RecommendedStrategyId = "Nexo.Linq",
                Reasoning = "LINQ provides excellent readability and functional programming support",
                DataSizeRange = (0, 50000),
                PerformanceCharacteristics = "Good performance with excellent readability"
            }
        };
    }
    
    private IEnumerable<StrategyRecommendation> GetWebAssemblyRecommendations()
    {
        return new[]
        {
            new StrategyRecommendation
            {
                Scenario = "WebAssembly performance optimization",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides best performance in WebAssembly environment",
                DataSizeRange = (0, 50000),
                PerformanceCharacteristics = "Optimized for WebAssembly execution"
            }
        };
    }
    
    private IEnumerable<StrategyRecommendation> GetMobileRecommendations()
    {
        return new[]
        {
            new StrategyRecommendation
            {
                Scenario = "Mobile performance optimization",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides best performance on mobile devices",
                DataSizeRange = (0, 10000),
                PerformanceCharacteristics = "Optimized for mobile CPU and memory constraints"
            }
        };
    }
    
    private IEnumerable<StrategyRecommendation> GetServerRecommendations()
    {
        return new[]
        {
            new StrategyRecommendation
            {
                Scenario = "Server-side parallel processing",
                RecommendedStrategyId = "Nexo.ParallelLinq",
                Reasoning = "Parallel LINQ leverages multiple CPU cores for server workloads",
                DataSizeRange = (1000, int.MaxValue),
                PerformanceCharacteristics = "Excellent scalability for server environments"
            },
            new StrategyRecommendation
            {
                Scenario = "High-throughput data processing",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides consistent performance for high-throughput scenarios",
                DataSizeRange = (0, 100000),
                PerformanceCharacteristics = "Consistent performance with low overhead"
            }
        };
    }
    
    private IEnumerable<StrategyRecommendation> GetDefaultRecommendations()
    {
        return new[]
        {
            new StrategyRecommendation
            {
                Scenario = "General-purpose iteration",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides good performance across most scenarios",
                DataSizeRange = (0, 100000),
                PerformanceCharacteristics = "Balanced performance and compatibility"
            }
        };
    }
}
