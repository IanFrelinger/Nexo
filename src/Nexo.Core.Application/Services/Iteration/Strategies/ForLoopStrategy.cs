using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Traditional for-loop iteration - maximum performance, requires IList
/// </summary>
public class ForLoopStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "ForLoop";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.High,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = int.MaxValue,
        SupportsParallelization = false,
        RequiresIList = true
    };
    
    public PlatformCompatibility PlatformCompatibility => 
        PlatformCompatibility.All;
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        var list = source as IList<T> ?? source.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            action(list[i]);
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        var list = source as IList<T> ?? source.ToList();
        var results = new TResult[list.Count];
        
        for (int i = 0; i < list.Count; i++)
        {
            results[i] = transform(list[i]);
        }
        
        return results;
    }
    
    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        var list = source as IList<T> ?? source.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            await asyncAction(list[i]);
        }
    }
    
    public IEnumerable<TResult> ExecuteWhere<TResult>(
        IEnumerable<T> source, 
        Func<T, bool> predicate, 
        Func<T, TResult> transform)
    {
        var list = source as IList<T> ?? source.ToList();
        var results = new List<TResult>();
        
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                results.Add(transform(list[i]));
            }
        }
        
        return results;
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return context.PlatformTarget switch
        {
            PlatformTarget.Unity => 
                GenerateUnityForLoop(context),
            PlatformTarget.Browser => 
                GenerateJavaScriptForLoop(context),
            PlatformTarget.Native => 
                GenerateSwiftForLoop(context),
            _ => GenerateCSharpForLoop(context)
        };
    }
    
    public bool CanHandle(IIterationPipelineContext context)
    {
        return context.HasValue("RequiresIList") || 
               context.GetValue<bool>("RequiresIList", false);
    }
    
    public int GetPriority(IIterationPipelineContext context)
    {
        var dataSize = context.GetValue<int>("DataSize", 0);
        var requiresPerformance = context.GetValue<bool>("RequiresPerformance", false);
        
        if (requiresPerformance && dataSize > 100)
            return 100;
        
        if (dataSize > 1000)
            return 90;
            
        return 70;
    }
    
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var baseTime = context.DataSize * 0.001; // 1ms per 1000 items
        var memoryUsage = context.DataSize * 0.001; // 1MB per 1000 items
        
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = baseTime,
            EstimatedMemoryUsageMB = memoryUsage,
            Confidence = 0.9,
            PerformanceScore = 95,
            MeetsRequirements = baseTime <= context.Requirements.ToPerformanceRequirements().MaxExecutionTimeMs &&
                              memoryUsage <= context.Requirements.ToPerformanceRequirements().MaxMemoryUsageMB
        };
    }
    
    private string GenerateCSharpForLoop(CodeGenerationContext context) =>
        $$"""
        for (int i = 0; i < {{context.CollectionVariableName}}.Count; i++)
        {
            {{context.ActionCode.Replace("{item}", $"{context.CollectionVariableName}[i]")}}
        }
        """;
    
    private string GenerateUnityForLoop(CodeGenerationContext context) =>
        $$"""
        for (int i = 0; i < {{context.CollectionVariableName}}.Count; i++)
        {
            var {{context.ItemVariableName}} = {{context.CollectionVariableName}}[i];
            {{context.ActionCode.Replace("{item}", context.ItemVariableName)}}
        }
        """;
    
    private string GenerateJavaScriptForLoop(CodeGenerationContext context) =>
        $$"""
        for (let i = 0; i < {{context.CollectionVariableName}}.length; i++) {
            {{context.ActionCode.Replace("{item}", $"{context.CollectionVariableName}[i]")}}
        }
        """;
    
    private string GenerateSwiftForLoop(CodeGenerationContext context) =>
        $$"""
        for i in 0..<{{context.CollectionVariableName}}.count {
            {{context.ActionCode.Replace("{item}", $"{context.CollectionVariableName}[i]")}}
        }
        """;
}
