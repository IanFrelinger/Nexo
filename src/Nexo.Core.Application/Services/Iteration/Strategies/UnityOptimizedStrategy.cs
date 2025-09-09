using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Unity-optimized iteration strategy - balances performance with Unity's constraints
/// </summary>
public class UnityOptimizedStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "UnityOptimized";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.Medium,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 100000,
        SupportsParallelization = false,
        RequiresIList = false
    };
    
    public PlatformCompatibility PlatformCompatibility => 
        PlatformCompatibility.Unity | PlatformCompatibility.Mobile;
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        // Unity prefers foreach for better GC behavior
        foreach (var item in source)
        {
            action(item);
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        // Pre-allocate capacity when possible to reduce GC pressure
        var list = source as IList<T>;
        var results = list != null ? new List<TResult>(list.Count) : new List<TResult>();
        
        foreach (var item in source)
        {
            results.Add(transform(item));
        }
        
        return results;
    }
    
    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        // Unity doesn't support true async iteration well, so we use synchronous approach
        foreach (var item in source)
        {
            await asyncAction(item);
        }
    }
    
    public IEnumerable<TResult> ExecuteWhere<TResult>(
        IEnumerable<T> source, 
        Func<T, bool> predicate, 
        Func<T, TResult> transform)
    {
        var results = new List<TResult>();
        foreach (var item in source)
        {
            if (predicate(item))
            {
                results.Add(transform(item));
            }
        }
        return results;
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return context.PlatformTarget switch
        {
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 
                GenerateUnityCode(context),
            _ => new ForeachStrategy<T>().GenerateCode(context)
        };
    }
    
    private string GenerateUnityCode(CodeGenerationContext context)
    {
        return $$"""
        // Unity-optimized iteration
        foreach (var {{context.ItemName}} in {{context.CollectionName}})
        {
            {{context.IterationBodyTemplate.Replace("{item}", context.ItemName)}}
        }
        """;
    }

    public bool CanHandle(IIterationPipelineContext context)
    {
        var platform = context.GetValue<PlatformTarget>("PlatformTarget", PlatformTarget.DotNet);
        return platform == PlatformTarget.Unity2022 || platform == PlatformTarget.Unity2023;
    }

    public int GetPriority(IIterationPipelineContext context)
    {
        var platform = context.GetValue<PlatformTarget>("PlatformTarget", PlatformTarget.DotNet);
        if (platform == PlatformTarget.Unity2022 || platform == PlatformTarget.Unity2023)
            return 95; // High priority for Unity platforms
        return 20; // Low priority for non-Unity platforms
    }

    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var baseTime = context.EstimatedDataSize * 0.001; // Unity-optimized performance
        var memoryUsage = context.EstimatedDataSize * 0.0008; // Lower memory usage for Unity
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = baseTime,
            EstimatedMemoryUsageMB = memoryUsage,
            Confidence = 0.85,
            PerformanceScore = 88,
            MeetsRequirements = context.EnvironmentProfile.CurrentPlatform == PlatformType.Unity2022 || 
                               context.EnvironmentProfile.CurrentPlatform == PlatformType.Unity2023
        };
    }
}
