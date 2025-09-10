using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Entities = Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Application.Services.Iteration.Strategies;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Simple iteration strategy selector implementation
/// </summary>
public class IterationStrategySelector : IIterationStrategySelector
{
    private readonly List<IIterationStrategy<object>> _strategies = new();
    private RuntimeEnvironmentProfile _environmentProfile = new(); // Default profile
    private readonly ILogger<IterationStrategySelector> _logger;

    public IterationStrategySelector(ILogger<IterationStrategySelector> logger)
    {
        _logger = logger;
        RegisterDefaultStrategies();
    }

    public IIterationStrategy<T> SelectStrategy<T>(IterationContext context)
    {
        var compatibleStrategies = _strategies
            .Where(s => s is IIterationStrategy<T> || (s is StrategyWrapper<T> wrapper && wrapper.CanHandleType<T>()))
            .Where(s => IsPlatformCompatible(s.PlatformCompatibility, context.EnvironmentProfile.CurrentPlatform))
            .Select(s => s is IIterationStrategy<T> direct ? direct : ((StrategyWrapper<T>)s).Unwrap())
            .ToList();

        _logger.LogDebug("Found {Count} compatible strategies for type {Type}", compatibleStrategies.Count, typeof(T).Name);
        
        if (!compatibleStrategies.Any())
        {
            _logger.LogWarning("No compatible strategies found, using fallback");
            // Fallback to a basic strategy if no specific one is found
            return new SimpleForeachStrategy<T>();
        }

        // Calculate scores and log them
        var scoredStrategies = compatibleStrategies
            .Select(s => new { Strategy = s, Score = CalculateSimpleScore(s, context) })
            .OrderByDescending(x => x.Score)
            .ToList();
            
        _logger.LogDebug("Strategy scores:");
        foreach (var scored in scoredStrategies)
        {
            _logger.LogDebug("  {StrategyId}: {Score}", scored.Strategy.StrategyId, scored.Score);
        }

        // Simple scoring: prioritize performance for larger data, readability for smaller
        return scoredStrategies.FirstOrDefault()?.Strategy ?? new SimpleForeachStrategy<T>();
    }

    public IIterationStrategy<T> SelectStrategy<T>(IEnumerable<T> source, IterationRequirements requirements)
    {
        var context = new IterationContext
        {
            DataSize = EstimateDataSize(source),
            Requirements = requirements,
            EnvironmentProfile = _environmentProfile // Use internal profile for simplicity
        };
        return SelectStrategy<T>(context);
    }

    public void RegisterStrategy<T>(IIterationStrategy<T> strategy)
    {
        // Create a wrapper strategy that can handle object types
        var wrapper = new StrategyWrapper<T>(strategy);
        _strategies.Add(wrapper);
    }

    public void SetEnvironmentProfile(RuntimeEnvironmentProfile profile)
    {
        _environmentProfile = profile;
    }

    public IEnumerable<IIterationStrategy<T>> GetAvailableStrategies<T>(IterationContext context)
    {
        return _strategies
            .OfType<IIterationStrategy<T>>()
            .Where(s => s.PlatformCompatibility.HasFlag(context.EnvironmentProfile.CurrentPlatform))
            .OrderByDescending(s => CalculateSimpleScore(s, context));
    }

    public string GetSelectionReasoning(IterationContext context)
    {
        var selected = SelectStrategy<object>(context);
        return $"Selected {selected.StrategyId} due to data size {context.DataSize} and requirements.";
    }

    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IIterationStrategy<object> strategy, IterationContext context)
    {
        // Simplified estimation
        var baseTime = context.DataSize * 0.001;
        var memoryUsage = context.DataSize * 0.001;
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = baseTime,
            EstimatedMemoryUsageMB = memoryUsage,
            Confidence = 0.8
        };
    }

    public IEnumerable<StrategyRecommendation> GetRecommendations(PlatformTarget platform)
    {
        var recommendations = new List<StrategyRecommendation>();
        
        switch (platform)
        {
            case PlatformTarget.Unity:
                recommendations.Add(new StrategyRecommendation
                {
                    RecommendedStrategyId = "Nexo.ForLoop",
                    Reasoning = "Unity-optimized for-loop for best performance",
                    Confidence = 0.95
                });
                break;
            case PlatformTarget.DotNet:
                recommendations.Add(new StrategyRecommendation
                {
                    RecommendedStrategyId = "Nexo.ParallelLinq",
                    Reasoning = "Parallel LINQ for CPU-intensive operations",
                    Confidence = 0.90
                });
                break;
            default:
                recommendations.Add(new StrategyRecommendation
                {
                    RecommendedStrategyId = "Nexo.Foreach",
                    Reasoning = "Good balance of performance and readability",
                    Confidence = 0.80
                });
                break;
        }
        
        return recommendations;
    }

    public Task<IEnumerable<StrategyComparisonResult>> CompareStrategies<T>(IterationContext context)
    {
        var results = GetAvailableStrategies<T>(context)
            .Select(s => new StrategyComparisonResult
            {
                Strategy = (IIterationStrategy<object>)s,
                PerformanceEstimate = EstimatePerformance((IIterationStrategy<object>)s, context),
                SuitabilityScore = CalculateSimpleScore(s, context),
                Reasoning = $"Score {CalculateSimpleScore(s, context)}",
                IsRecommended = true // Simplified
            })
            .ToList();
        return Task.FromResult<IEnumerable<StrategyComparisonResult>>(results);
    }


    private double CalculateSimpleScore<T>(IIterationStrategy<T> strategy, IterationContext context)
    {
        double score = 0;
        
        // Data size suitability
        if (strategy.PerformanceProfile.OptimalDataSizeMin <= context.DataSize &&
            strategy.PerformanceProfile.OptimalDataSizeMax >= context.DataSize)
        {
            score += 50;
        }
        
        // Parallelization requirement
        if (context.Requirements.RequiresParallelization && strategy.PerformanceProfile.SupportsParallelization)
        {
            score += 30;
        }
        
        // Memory priority
        if (context.Requirements.PrioritizeMemory)
        {
            score += (int)strategy.PerformanceProfile.MemoryEfficiency * 10;
        }
        
        // CPU priority - give much higher weight to CPU efficiency when CPU is prioritized
        if (context.Requirements.PrioritizeCpu)
        {
            score += (int)strategy.PerformanceProfile.CpuEfficiency * 50; // Increased from 10 to 50
        }
        
        // General performance scoring
        score += (int)strategy.PerformanceProfile.CpuEfficiency * 5;
        score += (int)strategy.PerformanceProfile.MemoryEfficiency * 5;
        score += (int)strategy.PerformanceProfile.Scalability * 3;
        
        // Debug logging
        _logger.LogDebug("Strategy {StrategyId}: CPU={CpuEfficiency}, Memory={MemoryEfficiency}, Score={Score}", 
            strategy.StrategyId, strategy.PerformanceProfile.CpuEfficiency, 
            strategy.PerformanceProfile.MemoryEfficiency, score);
        
        return score;
    }

    private void RegisterDefaultStrategies()
    {
        // Register high-performance strategies for int type using wrapper
        RegisterStrategy(new ForLoopStrategy<int>());
        RegisterStrategy(new ForeachStrategy<int>());
        RegisterStrategy(new LinqStrategy<int>());
        RegisterStrategy(new ParallelLinqStrategy<int>());
        RegisterStrategy(new UnityOptimizedStrategy<int>());
        RegisterStrategy(new WasmOptimizedStrategy<int>());
        
        // Register for object type as well
        RegisterStrategy(new ForLoopStrategy<object>());
        RegisterStrategy(new ForeachStrategy<object>());
        RegisterStrategy(new LinqStrategy<object>());
        RegisterStrategy(new ParallelLinqStrategy<object>());
        RegisterStrategy(new UnityOptimizedStrategy<object>());
        RegisterStrategy(new WasmOptimizedStrategy<object>());
        
        // Keep simple strategies as fallbacks
        RegisterStrategy(new SimpleForLoopStrategy<int>());
        RegisterStrategy(new SimpleForeachStrategy<int>());
        RegisterStrategy(new SimpleLinqStrategy<int>());
        RegisterStrategy(new SimpleForLoopStrategy<object>());
        RegisterStrategy(new SimpleForeachStrategy<object>());
        RegisterStrategy(new SimpleLinqStrategy<object>());
        
        _logger.LogInformation("Registered {Count} iteration strategies", _strategies.Count);
        foreach (var strategy in _strategies)
        {
            _logger.LogDebug("Registered strategy: {StrategyId} (CPU: {CpuEfficiency}, Memory: {MemoryEfficiency})", 
                strategy.StrategyId, strategy.PerformanceProfile.CpuEfficiency, strategy.PerformanceProfile.MemoryEfficiency);
        }
    }


    private int EstimateDataSize<T>(IEnumerable<T> source)
    {
        return source switch
        {
            ICollection<T> collection => collection.Count,
            _ => source.Take(1000).Count() // Sample for estimation
        };
    }

    /// <summary>
    /// Checks if a PlatformCompatibility enum is compatible with a PlatformType enum
    /// </summary>
    /// <param name="compatibility">The PlatformCompatibility flags</param>
    /// <param name="platformType">The PlatformType to check</param>
    /// <returns>True if compatible, false otherwise</returns>
    private static bool IsPlatformCompatible(Entities.PlatformCompatibility compatibility, PlatformType platformType)
    {
        // Handle special cases
        if (compatibility == Entities.PlatformCompatibility.All)
            return true;
            
        if (compatibility == Entities.PlatformCompatibility.None)
            return false;

        // Map PlatformType to PlatformCompatibility
        Entities.PlatformCompatibility mappedCompatibility = platformType switch
        {
            PlatformType.DotNet => Entities.PlatformCompatibility.DotNet,
            PlatformType.Unity or PlatformType.Unity2022 or PlatformType.Unity2023 => Entities.PlatformCompatibility.Unity,
            PlatformType.WebAssembly => Entities.PlatformCompatibility.WebAssembly,
            PlatformType.Mobile => Entities.PlatformCompatibility.Mobile,
            PlatformType.Server => Entities.PlatformCompatibility.Server,
            PlatformType.Browser or PlatformType.Web or PlatformType.JavaScript => Entities.PlatformCompatibility.Browser,
            PlatformType.Native or PlatformType.Windows or PlatformType.Linux or PlatformType.macOS => Entities.PlatformCompatibility.Native,
            _ => Entities.PlatformCompatibility.None
        };

        return compatibility.HasFlag(mappedCompatibility);
    }
}

/// <summary>
/// Simple foreach strategy implementation
/// </summary>
public class SimpleForeachStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "SimpleForeach";
    public string Name => "Simple Foreach";
    public string Description => "Simple foreach loop implementation";
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.All;
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 1000,
        CpuEfficiency = PerformanceLevel.Medium,
        MemoryEfficiency = PerformanceLevel.High, // High memory efficiency for memory-prioritized scenarios
        Scalability = PerformanceLevel.Medium,
        SupportsParallelization = false
    };

    public bool CanHandle(IIterationPipelineContext context) => true;
    public int GetPriority(IIterationPipelineContext context) => 5;
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context) => new() { EstimatedExecutionTimeMs = context.DataSize * 0.001, EstimatedMemoryUsageMB = context.DataSize * 0.001, Confidence = 0.8 };

    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        var results = new List<TResult>();
        foreach (var item in source)
        {
            results.Add(transform(item));
        }
        return results;
    }

    public IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
    {
        var results = new List<TResult>();
        foreach (var item in source)
        {
            if (predicate(item))
            {
                results.Add(selector(item));
            }
        }
        return results;
    }

    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        foreach (var item in source)
        {
            await asyncAction(item);
        }
    }

    public Task<TResult> ExecuteAsync<TResult>(IEnumerable<T> source, Func<T, TResult> selector)
    {
        var results = new List<TResult>();
        foreach (var item in source)
        {
            results.Add(selector(item));
        }
        return Task.FromResult(results.FirstOrDefault());
    }

    public Task ExecuteAsync(IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
        return Task.CompletedTask;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        return $@"
foreach (var {context.ItemVariableName} in {context.CollectionVariableName})
{{
    {context.ActionCode}
}}";
    }
}

/// <summary>
/// Simple for loop strategy implementation
/// </summary>
public class SimpleForLoopStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "SimpleForLoop";
    public string Name => "Simple For Loop";
    public string Description => "Simple for loop implementation";
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.All;
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        OptimalDataSizeMin = 100,
        OptimalDataSizeMax = 10000,
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.High,
        SupportsParallelization = true
    };

    public bool CanHandle(IIterationPipelineContext context) => true;
    public int GetPriority(IIterationPipelineContext context) => 8;
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context) => new() { EstimatedExecutionTimeMs = context.DataSize * 0.0005, EstimatedMemoryUsageMB = context.DataSize * 0.0005, Confidence = 0.9 };

    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        var list = source.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            action(list[i]);
        }
    }

    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        var list = source.ToList();
        var results = new TResult[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            results[i] = transform(list[i]);
        }
        return results;
    }

    public IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
    {
        var list = source.ToList();
        var results = new List<TResult>();
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                results.Add(selector(list[i]));
            }
        }
        return results;
    }

    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        var list = source.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            await asyncAction(list[i]);
        }
    }

    public Task<TResult> ExecuteAsync<TResult>(IEnumerable<T> source, Func<T, TResult> selector)
    {
        var list = source.ToList();
        var results = new List<TResult>();
        for (int i = 0; i < list.Count; i++)
        {
            results.Add(selector(list[i]));
        }
        return Task.FromResult(results.FirstOrDefault());
    }

    public Task ExecuteAsync(IEnumerable<T> source, Action<T> action)
    {
        var list = source.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            action(list[i]);
        }
        return Task.CompletedTask;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        return $@"
for (int i = 0; i < {context.CollectionVariableName}.Count; i++)
{{
    var {context.ItemVariableName} = {context.CollectionVariableName}[i];
    {context.ActionCode}
}}";
    }
}

/// <summary>
/// Simple LINQ strategy implementation
/// </summary>
public class SimpleLinqStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "SimpleLinq";
    public string Name => "Simple LINQ";
    public string Description => "Simple LINQ implementation";
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.All;
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 5000,
        CpuEfficiency = PerformanceLevel.Medium,
        MemoryEfficiency = PerformanceLevel.Low,
        Scalability = PerformanceLevel.Medium,
        SupportsParallelization = true
    };

    public bool CanHandle(IIterationPipelineContext context) => true;
    public int GetPriority(IIterationPipelineContext context) => 6;
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context) => new() { EstimatedExecutionTimeMs = context.DataSize * 0.001, EstimatedMemoryUsageMB = context.DataSize * 0.001, Confidence = 0.8 };

    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        source.ToList().ForEach(action);
    }

    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        return source.Select(transform);
    }

    public IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
    {
        return source.Where(predicate).Select(selector);
    }

    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        var tasks = source.Select(asyncAction);
        await Task.WhenAll(tasks);
    }

    public Task<TResult> ExecuteAsync<TResult>(IEnumerable<T> source, Func<T, TResult> selector)
    {
        var result = source.Select(selector).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task ExecuteAsync(IEnumerable<T> source, Action<T> action)
    {
        source.ToList().ForEach(action);
        return Task.CompletedTask;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        return $@"
{context.CollectionVariableName}.Select({context.ItemVariableName} => 
{{
    {context.ActionCode}
    return {context.ItemVariableName};
}}).ToList();";
    }
}

/// <summary>
/// Wrapper strategy to convert IIterationStrategy<T> to IIterationStrategy<object>
/// </summary>
internal class StrategyWrapper<T> : IIterationStrategy<object>
{
    private readonly IIterationStrategy<T> _wrappedStrategy;

    public StrategyWrapper(IIterationStrategy<T> wrappedStrategy)
    {
        _wrappedStrategy = wrappedStrategy;
    }

    public string StrategyId => _wrappedStrategy.StrategyId;
    public PlatformCompatibility PlatformCompatibility => _wrappedStrategy.PlatformCompatibility;
    public IterationPerformanceProfile PerformanceProfile => _wrappedStrategy.PerformanceProfile;

    public void Execute(IEnumerable<object> source, Action<object> action)
    {
        var typedSource = source.OfType<T>();
        _wrappedStrategy.Execute(typedSource, item => action(item));
    }

    public IEnumerable<TResult> Execute<TResult>(IEnumerable<object> source, Func<object, TResult> transform)
    {
        var typedSource = source.OfType<T>();
        return _wrappedStrategy.Execute(typedSource, item => transform(item));
    }

    public IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<object> source, Func<object, bool> predicate, Func<object, TResult> selector)
    {
        var typedSource = source.OfType<T>();
        return _wrappedStrategy.ExecuteWhere(typedSource, item => predicate(item), item => selector(item));
    }

    public async Task ExecuteAsync(IEnumerable<object> source, Func<object, Task> asyncAction)
    {
        var typedSource = source.OfType<T>();
        await _wrappedStrategy.ExecuteAsync(typedSource, async item => await asyncAction(item));
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        return _wrappedStrategy.GenerateCode(context);
    }

    public bool CanHandle(IIterationPipelineContext context)
    {
        return _wrappedStrategy.CanHandle(context);
    }

    public int GetPriority(IIterationPipelineContext context)
    {
        return _wrappedStrategy.GetPriority(context);
    }

    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        return _wrappedStrategy.EstimatePerformance(context);
    }

    public bool CanHandleType<TTarget>()
    {
        return typeof(T) == typeof(TTarget);
    }

    public IIterationStrategy<T> Unwrap()
    {
        return _wrappedStrategy;
    }
}