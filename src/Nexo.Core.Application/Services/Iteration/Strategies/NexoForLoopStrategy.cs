using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// High-performance for-loop iteration strategy optimized for various platforms
/// </summary>
public class NexoForLoopStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "Nexo.ForLoop";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.High,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = int.MaxValue,
        SupportsParallelization = false,
        RequiresIList = true,
        SupportsAsync = false,
        SuitableForRealTime = true
    };
    
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.All;
    
    public bool CanHandle(IIterationPipelineContext context)
    {
        return context.DataSize <= 1000000 && !context.RequiresParallelization;
    }
    
    public int GetPriority(IIterationPipelineContext context)
    {
        if (context.Priority == (int)IterationPriority.Performance) return 95;
        if (context.Priority == (int)IterationPriority.Readability) return 50;
        if (context.Priority == (int)IterationPriority.Maintainability) return 60;
        return 80;
    }
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        if (source is IList<T> list)
        {
            // Optimized for IList - direct index access
            for (int i = 0; i < list.Count; i++)
            {
                action(list[i]);
            }
        }
        else if (source is T[] array)
        {
            // Optimized for arrays
            for (int i = 0; i < array.Length; i++)
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
            // Pre-allocate result array for better performance
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
    
    public IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
    {
        if (source is IList<T> list)
        {
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
        else if (source is T[] array)
        {
            var results = new List<TResult>();
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                {
                    results.Add(selector(array[i]));
                }
            }
            return results;
        }
        else
        {
            return source.Where(predicate).Select(selector);
        }
    }
    
    public Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        // For-loop doesn't support async operations efficiently
        throw new NotSupportedException("For-loop strategy does not support async operations. Use ParallelLinqStrategy instead.");
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return context.PlatformTarget switch
        {
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 
                GenerateUnityOptimizedForLoop(context),
            PlatformTarget.JavaScript => 
                GenerateJavaScriptForLoop(context),
            PlatformTarget.Swift => 
                GenerateSwiftForLoop(context),
            PlatformTarget.Kotlin => 
                GenerateKotlinForLoop(context),
            _ => GenerateCSharpForLoop(context)
        };
    }
    
    
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var dataSize = context.DataSize;
        var isCpuBound = context.IsCpuBound;
        var platform = context.TargetPlatform;
        
        // For-loop has excellent performance characteristics
        var baseTimePerItem = 0.001; // 1 microsecond per item (very fast)
        
        // Adjust for platform
        var platformMultiplier = platform switch
        {
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 1.2, // Slightly slower in Unity
            PlatformTarget.JavaScript => 2.0, // JavaScript is slower
            PlatformTarget.Swift => 0.8, // Swift is very fast
            PlatformTarget.Kotlin => 1.1, // Kotlin is fast
            _ => 1.0 // C# is fast
        };
        
        var estimatedTime = dataSize * baseTimePerItem * platformMultiplier;
        var estimatedMemory = dataSize * 0.001; // Very low memory overhead
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = estimatedTime,
            EstimatedMemoryUsageMB = estimatedMemory,
            Confidence = 0.9,
            PerformanceScore = CalculatePerformanceScore(estimatedTime, estimatedMemory, context),
            MeetsRequirements = estimatedTime <= context.Requirements.ToPerformanceRequirements().MaxExecutionTimeMs &&
                               estimatedMemory <= context.Requirements.ToPerformanceRequirements().MaxMemoryUsageMB
        };
    }
    
    private string GenerateCSharpForLoop(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        var nullCheck = context.IncludeNullChecks ? $"if ({collectionVar} != null)" : "";
        var boundsCheck = context.IncludeBoundsChecking ? " && i < {collectionVar}.Count" : "";
        
        return $@"{nullCheck}
{{
    for (int i = 0; i < {collectionVar}.Count{boundsCheck}; i++)
    {{
        var {itemVar} = {collectionVar}[i];
        {action}
    }}
}}";
    }
    
    private string GenerateUnityOptimizedForLoop(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Unity-optimized for-loop
if ({collectionVar} != null)
{{
    var count = {collectionVar}.Count;
    for (int i = 0; i < count; i++)
    {{
        var {itemVar} = {collectionVar}[i];
        {action}
    }}
}}";
    }
    
    private string GenerateJavaScriptForLoop(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// JavaScript for-loop
if ({collectionVar} !== null && {collectionVar} !== undefined)
{{
    for (let i = 0; i < {collectionVar}.length; i++)
    {{
        const {itemVar} = {collectionVar}[i];
        {action}
    }}
}}";
    }
    
    private string GenerateSwiftForLoop(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Swift for-loop
if let {collectionVar} = {collectionVar}
{{
    for i in 0..<{collectionVar}.count
    {{
        let {itemVar} = {collectionVar}[i]
        {action}
    }}
}}";
    }
    
    private string GenerateKotlinForLoop(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Kotlin for-loop
{collectionVar}?.let {{
    for (i in 0 until it.size)
    {{
        val {itemVar} = it[i]
        {action}
    }}
}}";
    }
    
    private PlatformCompatibility GetPlatformFlag(PlatformTarget platform)
    {
        return platform switch
        {
            PlatformTarget.DotNet => PlatformCompatibility.DotNet,
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => PlatformCompatibility.Unity,
            PlatformTarget.WebAssembly => PlatformCompatibility.WebAssembly,
            PlatformTarget.JavaScript => PlatformCompatibility.Browser,
            PlatformTarget.Swift or PlatformTarget.Kotlin => PlatformCompatibility.Mobile,
            _ => PlatformCompatibility.All
        };
    }
    
    
    private double CalculatePerformanceScore(double executionTime, double memoryUsage, IterationContext context)
    {
        // Calculate performance score based on execution time and memory usage
        var timeScore = Math.Max(0, 100 - (executionTime / context.DataSize) * 1000);
        var memoryScore = Math.Max(0, 100 - memoryUsage * 10);
        
        return (timeScore + memoryScore) / 2;
    }
}
