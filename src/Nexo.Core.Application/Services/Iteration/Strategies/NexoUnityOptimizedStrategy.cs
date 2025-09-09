using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Unity-optimized iteration strategy with game development specific optimizations
/// </summary>
public class NexoUnityOptimizedStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "Nexo.UnityOptimized";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.High,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 10000,
        SupportsParallelization = false,
        RequiresIList = true,
        SupportsAsync = false,
        SuitableForRealTime = true
    };
    
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.Unity;
    
    public bool CanHandle(IIterationPipelineContext context)
    {
        return context.PlatformTarget == PlatformTarget.Unity && context.DataSize <= 100000;
    }
    
    public int GetPriority(IIterationPipelineContext context)
    {
        if (context.PlatformTarget == PlatformTarget.Unity && context.Priority == (int)IterationPriority.Performance) return 95;
        if (context.PlatformTarget == PlatformTarget.Unity) return 80;
        return 0;
    }
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        if (source is IList<T> list)
        {
            // Unity-optimized for-loop with minimal allocations
            var count = list.Count;
            for (int i = 0; i < count; i++)
            {
                action(list[i]);
            }
        }
        else if (source is T[] array)
        {
            // Array optimization
            var length = array.Length;
            for (int i = 0; i < length; i++)
            {
                action(array[i]);
            }
        }
        else
        {
            // Fallback to foreach for other enumerables
            foreach (var item in source)
            {
                action(item);
            }
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        if (source is IList<T> list)
        {
            // Pre-allocate array to avoid List<T> allocations
            var results = new TResult[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                results[i] = transform(list[i]);
            }
            return results;
        }
        else if (source is T[] array)
        {
            var results = new TResult[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                results[i] = transform(array[i]);
            }
            return results;
        }
        else
        {
            // Fallback to LINQ for other enumerables
            return source.Select(transform);
        }
    }
    
    public Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        // Unity doesn't support async operations efficiently
        throw new NotSupportedException("Unity-optimized strategy does not support async operations.");
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return GenerateUnityOptimizedCode(context);
    }
    
    
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var dataSize = context.DataSize;
        var platform = context.TargetPlatform;
        
        // Unity-optimized performance characteristics
        var baseTimePerItem = 0.0008; // 0.8 microseconds per item (very fast)
        
        // Unity-specific optimizations
        var unityMultiplier = 1.0; // Unity-optimized code is very fast
        
        var estimatedTime = dataSize * baseTimePerItem * unityMultiplier;
        var estimatedMemory = dataSize * 0.0005; // Very low memory overhead
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = estimatedTime,
            EstimatedMemoryUsageMB = estimatedMemory,
            Confidence = 0.95,
            PerformanceScore = CalculatePerformanceScore(estimatedTime, estimatedMemory, context),
            MeetsRequirements = estimatedTime <= context.Requirements.MaxExecutionTimeMs &&
                               estimatedMemory <= context.Requirements.MaxMemoryUsageMB
        };
    }
    
    private string GenerateUnityOptimizedCode(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Unity-optimized iteration code
// This code is optimized for Unity's performance characteristics
if ({collectionVar} != null)
{{
    // Use for-loop for best performance in Unity
    // Avoid foreach and LINQ in performance-critical code
    var count = {collectionVar}.Count;
    for (int i = 0; i < count; i++)
    {{
        var {itemVar} = {collectionVar}[i];
        {action}
    }}
    
    // Unity-specific optimizations:
    // - Pre-calculate count to avoid repeated property access
    // - Use for-loop instead of foreach for better performance
    // - Minimize allocations in the loop
    // - Avoid LINQ in performance-critical paths
}}";
    }
    
    
    private double CalculatePerformanceScore(double executionTime, double memoryUsage, IterationContext context)
    {
        var timeScore = Math.Max(0, 100 - (executionTime / context.DataSize) * 1000);
        var memoryScore = Math.Max(0, 100 - memoryUsage * 10);
        
        return (timeScore + memoryScore) / 2;
    }
}
