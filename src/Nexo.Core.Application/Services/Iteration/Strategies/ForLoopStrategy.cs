using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Traditional for-loop iteration - maximum performance, requires IList
/// </summary>
public class ForLoopStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "ForLoop";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Excellent,
        MemoryEfficiency = PerformanceLevel.Excellent,
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
            PlatformTarget.Unity2022 or PlatformTarget.Unity2023 => 
                GenerateUnityForLoop(context),
            PlatformTarget.JavaScript => 
                GenerateJavaScriptForLoop(context),
            PlatformTarget.Swift => 
                GenerateSwiftForLoop(context),
            _ => GenerateCSharpForLoop(context)
        };
    }
    
    private string GenerateCSharpForLoop(CodeGenerationContext context) =>
        $$"""
        for (int i = 0; i < {{context.CollectionName}}.Count; i++)
        {
            {{context.IterationBodyTemplate.Replace("{item}", $"{context.CollectionName}[i]")}}
        }
        """;
    
    private string GenerateUnityForLoop(CodeGenerationContext context) =>
        $$"""
        for (int i = 0; i < {{context.CollectionName}}.Count; i++)
        {
            var {{context.ItemName}} = {{context.CollectionName}}[i];
            {{context.IterationBodyTemplate.Replace("{item}", context.ItemName)}}
        }
        """;
    
    private string GenerateJavaScriptForLoop(CodeGenerationContext context) =>
        $$"""
        for (let i = 0; i < {{context.CollectionName}}.length; i++) {
            {{context.IterationBodyTemplate.Replace("{item}", $"{context.CollectionName}[i]")}}
        }
        """;
    
    private string GenerateSwiftForLoop(CodeGenerationContext context) =>
        $$"""
        for i in 0..<{{context.CollectionName}}.count {
            {{context.IterationBodyTemplate.Replace("{item}", $"{context.CollectionName}[i]")}}
        }
        """;
}
