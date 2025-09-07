using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// LINQ-based iteration - excellent for functional composition and readability
/// </summary>
public class LinqStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "Linq";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Medium,
        MemoryEfficiency = PerformanceLevel.Medium,
        Scalability = PerformanceLevel.Medium,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 50000,
        SupportsParallelization = false,
        RequiresIList = false
    };
    
    public PlatformCompatibility PlatformCompatibility => 
        PlatformCompatibility.DotNet | PlatformCompatibility.Unity | PlatformCompatibility.Server;
    
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
        await Task.WhenAll(source.Select(asyncAction));
    }
    
    public IEnumerable<TResult> ExecuteWhere<TResult>(
        IEnumerable<T> source, 
        Func<T, bool> predicate, 
        Func<T, TResult> transform)
    {
        return source.Where(predicate).Select(transform);
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        if (context.HasWhere && context.HasSelect)
        {
            return $"{context.CollectionName}.Where({context.PredicateTemplate}).Select({context.TransformTemplate})";
        }
        else if (context.HasSelect)
        {
            return $"{context.CollectionName}.Select({context.TransformTemplate})";
        }
        else if (context.HasWhere)
        {
            return $"{context.CollectionName}.Where({context.PredicateTemplate}).ToList().ForEach({context.ActionTemplate})";
        }
        else
        {
            return $"{context.CollectionName}.ToList().ForEach({context.ActionTemplate})";
        }
    }
}
