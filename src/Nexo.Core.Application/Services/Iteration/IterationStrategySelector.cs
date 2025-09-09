using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Entities.Iteration;
using Entities = Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

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
            .OfType<IIterationStrategy<T>>()
            .Where(s => s.PlatformCompatibility.HasFlag(context.EnvironmentProfile.CurrentPlatform))
            .ToList();

        if (!compatibleStrategies.Any())
        {
            // Fallback to a basic strategy if no specific one is found
            return new SimpleForeachStrategy<T>();
        }

        // Simple scoring: prioritize performance for larger data, readability for smaller
        return compatibleStrategies
            .OrderByDescending(s => CalculateSimpleScore(s, context))
            .FirstOrDefault() ?? new SimpleForeachStrategy<T>();
    }

    public IIterationStrategy<T> SelectStrategy<T>(IEnumerable<T> source, Nexo.Core.Domain.Entities.Iteration.PerformanceRequirements requirements)
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
        _strategies.Add((IIterationStrategy<object>)strategy);
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
        if (strategy.PerformanceProfile.OptimalDataSizeMin <= context.DataSize &&
            strategy.PerformanceProfile.OptimalDataSizeMax >= context.DataSize)
        {
            score += 50;
        }
        if (context.Requirements.PreferParallel && strategy.PerformanceProfile.SupportsParallelization)
        {
            score += 30;
        }
        // Add more simple scoring logic here
        return score;
    }

    private void RegisterDefaultStrategies()
    {
        _strategies.Add(new SimpleForLoopStrategy<object>());
        _strategies.Add(new SimpleForeachStrategy<object>());
        _strategies.Add(new SimpleLinqStrategy<object>());
    }


    private int EstimateDataSize<T>(IEnumerable<T> source)
    {
        return source switch
        {
            ICollection<T> collection => collection.Count,
            _ => source.Take(1000).Count() // Sample for estimation
        };
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
        MemoryEfficiency = PerformanceLevel.Medium,
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