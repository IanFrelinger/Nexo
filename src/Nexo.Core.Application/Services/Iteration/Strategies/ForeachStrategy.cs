using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Foreach iteration - good balance of performance and readability
/// </summary>
public class ForeachStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "Foreach";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.High,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = int.MaxValue,
        SupportsParallelization = false,
        RequiresIList = false
    };
    
    public PlatformCompatibility PlatformCompatibility => 
        PlatformCompatibility.All;
    
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
            PlatformTarget.JavaScript => 
                $"{context.CollectionName}.forEach(({context.ItemName}) => {{\n    {context.IterationBodyTemplate.Replace("{item}", context.ItemName)}\n}})",
            PlatformTarget.Swift => 
                $"for {context.ItemName} in {context.CollectionName} {{\n    {context.IterationBodyTemplate.Replace("{item}", context.ItemName)}\n}}",
            PlatformTarget.Python => 
                $"for {context.ItemName} in {context.CollectionName}:\n    {context.IterationBodyTemplate.Replace("{item}", context.ItemName)}",
            _ => 
                $"foreach (var {context.ItemName} in {context.CollectionName})\n{{\n    {context.IterationBodyTemplate.Replace("{item}", context.ItemName)}\n}}"
        };
    }

    public bool CanHandle(IIterationPipelineContext context)
    {
        return true; // Foreach can handle any IEnumerable
    }

    public int GetPriority(IIterationPipelineContext context)
    {
        var dataSize = context.GetValue<int>("DataSize", 0);
        var requiresReadability = context.GetValue<bool>("RequiresReadability", false);
        
        if (requiresReadability)
            return 90;
        
        if (dataSize < 1000)
            return 80;
            
        return 60;
    }

    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var baseTime = context.EstimatedDataSize * 0.0015; // Slightly slower than for-loop
        var memoryUsage = context.EstimatedDataSize * 0.001;
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = baseTime,
            EstimatedMemoryUsageMB = memoryUsage,
            Confidence = 0.85,
            PerformanceScore = 85,
            MeetsRequirements = baseTime <= context.Requirements.ToPerformanceRequirements().MaxExecutionTimeMs &&
                              memoryUsage <= context.Requirements.ToPerformanceRequirements().MaxMemoryUsageMB
        };
    }
}
