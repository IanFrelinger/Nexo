using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Parallel LINQ - optimal for CPU-intensive operations on multi-core systems
/// </summary>
public class ParallelLinqStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "ParallelLinq";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.Medium,
        Scalability = PerformanceLevel.High,
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

    public bool CanHandle(IIterationPipelineContext context)
    {
        var dataSize = context.GetValue<int>("DataSize", 0);
        var requiresParallel = context.GetValue<bool>("RequiresParallel", false);
        
        return requiresParallel && dataSize >= 1000; // Parallel LINQ is good for large datasets
    }

    public int GetPriority(IIterationPipelineContext context)
    {
        var dataSize = context.GetValue<int>("DataSize", 0);
        var requiresParallel = context.GetValue<bool>("RequiresParallel", false);
        
        if (requiresParallel && dataSize >= 1000)
            return 90; // High priority for large parallel operations
        return 30; // Lower priority for smaller datasets
    }

    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var baseTime = context.EstimatedDataSize * 0.0005; // Parallel processing is faster
        var memoryUsage = context.EstimatedDataSize * 0.002; // Higher memory usage due to parallelization
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = baseTime,
            EstimatedMemoryUsageMB = memoryUsage,
            Confidence = 0.9,
            PerformanceScore = 95,
            MeetsRequirements = context.Requirements.PreferParallel
        };
    }
}
