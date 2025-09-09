using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// LINQ iteration strategy with excellent readability and functional programming support
/// </summary>
public class NexoLinqStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "Nexo.Linq";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Medium,
        MemoryEfficiency = PerformanceLevel.Medium,
        Scalability = PerformanceLevel.High,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 50000,
        SupportsParallelization = false,
        RequiresIList = false,
        SupportsAsync = false,
        SuitableForRealTime = false
    };
    
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.DotNet | PlatformCompatibility.Unity;
    
    public bool CanHandle(IIterationPipelineContext context)
    {
        return context.DataSize <= 50000 && !context.RequiresParallelization;
    }
    
    public int GetPriority(IIterationPipelineContext context)
    {
        if (context.Priority == (int)IterationPriority.Readability) return 95;
        if (context.Priority == (int)IterationPriority.Maintainability) return 90;
        if (context.Priority == (int)IterationPriority.Performance) return 40;
        return 70;
    }
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        // LINQ doesn't have a direct equivalent to foreach with action
        // Use ToList() to materialize and then foreach
        foreach (var item in source.ToList())
        {
            action(item);
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        return source.Select(transform);
    }
    
    public Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        // LINQ doesn't support async operations efficiently
        throw new NotSupportedException("LINQ strategy does not support async operations. Use ParallelLinqStrategy instead.");
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return context.PlatformTarget switch
        {
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 
                GenerateUnityOptimizedLinq(context),
            _ => GenerateCSharpLinq(context)
        };
    }
    
    
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var dataSize = context.DataSize;
        var platform = context.TargetPlatform;
        
        // LINQ has moderate performance due to overhead
        var baseTimePerItem = 0.005; // 5 microseconds per item
        
        // Adjust for platform
        var platformMultiplier = platform switch
        {
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 2.0, // Unity LINQ is slower
            _ => 1.0 // C# LINQ is reasonable
        };
        
        var estimatedTime = dataSize * baseTimePerItem * platformMultiplier;
        var estimatedMemory = dataSize * 0.01; // Higher memory overhead due to LINQ overhead
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = estimatedTime,
            EstimatedMemoryUsageMB = estimatedMemory,
            Confidence = 0.8,
            PerformanceScore = CalculatePerformanceScore(estimatedTime, estimatedMemory, context),
            MeetsRequirements = estimatedTime <= context.Requirements.MaxExecutionTimeMs &&
                               estimatedMemory <= context.Requirements.MaxMemoryUsageMB
        };
    }
    
    private string GenerateCSharpLinq(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        var nullCheck = context.IncludeNullChecks ? $"if ({collectionVar} != null)" : "";
        
        return $@"{nullCheck}
{{
    // LINQ approach - good for readability, moderate performance
    var results = {collectionVar}
        .Select({itemVar} => {{
            {action}
            return {itemVar}; // Return the item for further processing
        }})
        .ToList(); // Materialize the results
}}";
    }
    
    private string GenerateUnityOptimizedLinq(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Unity LINQ (note: slower than for-loop in Unity)
if ({collectionVar} != null)
{{
    // Use LINQ for readability, but consider for-loop for performance
    var results = {collectionVar}
        .Select({itemVar} => {{
            {action}
            return {itemVar};
        }})
        .ToList();
}}";
    }
    
    private PlatformCompatibility GetPlatformFlag(PlatformTarget platform)
    {
        return platform switch
        {
            PlatformTarget.DotNet => PlatformCompatibility.DotNet,
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => PlatformCompatibility.Unity,
            _ => PlatformCompatibility.None
        };
    }
    
    
    private double CalculatePerformanceScore(double executionTime, double memoryUsage, IterationContext context)
    {
        var timeScore = Math.Max(0, 100 - (executionTime / context.DataSize) * 1000);
        var memoryScore = Math.Max(0, 100 - memoryUsage * 10);
        
        return (timeScore + memoryScore) / 2;
    }
}
