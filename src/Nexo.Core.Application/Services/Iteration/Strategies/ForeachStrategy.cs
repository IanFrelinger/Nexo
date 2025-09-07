using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;

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
}
