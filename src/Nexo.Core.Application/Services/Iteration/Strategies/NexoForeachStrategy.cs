using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Foreach iteration strategy with good readability and moderate performance
/// </summary>
public class NexoForeachStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "Nexo.Foreach";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.Medium,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 100000,
        SupportsParallelization = false,
        RequiresIList = false,
        SupportsAsync = false,
        SuitableForRealTime = true
    };
    
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.All;
    
    public bool CanHandle(IIterationPipelineContext context)
    {
        return context.DataSize <= 100000 && !context.RequiresParallelization;
    }
    
    public int GetPriority(IIterationPipelineContext context)
    {
        if (context.Priority == (int)IterationPriority.Readability) return 90;
        if (context.Priority == (int)IterationPriority.Performance) return 60;
        if (context.Priority == (int)IterationPriority.Maintainability) return 80;
        return 70;
    }
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
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
        // Foreach doesn't support async operations efficiently
        throw new NotSupportedException("Foreach strategy does not support async operations. Use ParallelLinqStrategy instead.");
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return context.PlatformTarget switch
        {
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 
                GenerateUnityOptimizedForeach(context),
            PlatformTarget.JavaScript => 
                GenerateJavaScriptForeach(context),
            PlatformTarget.Swift => 
                GenerateSwiftForeach(context),
            PlatformTarget.Kotlin => 
                GenerateKotlinForeach(context),
            _ => GenerateCSharpForeach(context)
        };
    }
    
    
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var dataSize = context.DataSize;
        var platform = context.TargetPlatform;
        
        // Foreach has good performance but not as fast as for-loop
        var baseTimePerItem = 0.002; // 2 microseconds per item
        
        // Adjust for platform
        var platformMultiplier = platform switch
        {
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 1.5, // Unity foreach is slower
            PlatformTarget.JavaScript => 2.5, // JavaScript foreach is slower
            PlatformTarget.Swift => 1.0, // Swift foreach is good
            PlatformTarget.Kotlin => 1.2, // Kotlin foreach is good
            _ => 1.0 // C# foreach is good
        };
        
        var estimatedTime = dataSize * baseTimePerItem * platformMultiplier;
        var estimatedMemory = dataSize * 0.002; // Low memory overhead
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = estimatedTime,
            EstimatedMemoryUsageMB = estimatedMemory,
            Confidence = 0.85,
            PerformanceScore = CalculatePerformanceScore(estimatedTime, estimatedMemory, context),
            MeetsRequirements = estimatedTime <= context.Requirements.MaxExecutionTimeMs &&
                               estimatedMemory <= context.Requirements.MaxMemoryUsageMB
        };
    }
    
    private string GenerateCSharpForeach(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        var nullCheck = context.IncludeNullChecks ? $"if ({collectionVar} != null)" : "";
        
        return $@"{nullCheck}
{{
    foreach (var {itemVar} in {collectionVar})
    {{
        {action}
    }}
}}";
    }
    
    private string GenerateUnityOptimizedForeach(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Unity foreach (note: slower than for-loop in Unity)
if ({collectionVar} != null)
{{
    foreach (var {itemVar} in {collectionVar})
    {{
        {action}
    }}
}}";
    }
    
    private string GenerateJavaScriptForeach(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// JavaScript foreach
if ({collectionVar} !== null && {collectionVar} !== undefined)
{{
    {collectionVar}.forEach(({itemVar}) => {{
        {action}
    }});
}}";
    }
    
    private string GenerateSwiftForeach(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Swift foreach
if let {collectionVar} = {collectionVar}
{{
    for {itemVar} in {collectionVar}
    {{
        {action}
    }}
}}";
    }
    
    private string GenerateKotlinForeach(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Kotlin foreach
{collectionVar}?.forEach {{ {itemVar} ->
    {action}
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
        var timeScore = Math.Max(0, 100 - (executionTime / context.DataSize) * 1000);
        var memoryScore = Math.Max(0, 100 - memoryUsage * 10);
        
        return (timeScore + memoryScore) / 2;
    }
}
