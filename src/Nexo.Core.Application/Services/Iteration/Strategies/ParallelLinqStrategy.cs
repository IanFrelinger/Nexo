using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Parallel LINQ - optimal for CPU-intensive operations on multi-core systems
/// </summary>
public class ParallelLinqStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "ParallelLinq";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Excellent,
        MemoryEfficiency = PerformanceLevel.Medium,
        Scalability = PerformanceLevel.Excellent,
        OptimalDataSizeMin = 1000,
        OptimalDataSizeMax = int.MaxValue,
        SupportsParallelization = true,
        RequiresIList = false
    };
    
    public PlatformCompatibility PlatformCompatibility => 
        PlatformCompatibility.DotNet | PlatformCompatibility.Server;
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        source.AsParallel().ForAll(action);
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        return source.AsParallel().Select(transform);
    }
    
    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        var tasks = source.AsParallel().Select(asyncAction);
        await Task.WhenAll(tasks);
    }
    
    public IEnumerable<TResult> ExecuteWhere<TResult>(
        IEnumerable<T> source, 
        Func<T, bool> predicate, 
        Func<T, TResult> transform)
    {
        return source.AsParallel().Where(predicate).Select(transform);
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        var baseLinq = new LinqStrategy<T>().GenerateCode(context);
        return baseLinq.Replace(context.CollectionName, $"{context.CollectionName}.AsParallel()");
    }
}
