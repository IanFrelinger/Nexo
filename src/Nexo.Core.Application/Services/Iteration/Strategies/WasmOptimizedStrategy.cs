using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// WebAssembly-optimized iteration strategy - memory-conscious for WASM constraints
/// </summary>
public class WasmOptimizedStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "WasmOptimized";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Medium,
        MemoryEfficiency = PerformanceLevel.Excellent,
        Scalability = PerformanceLevel.Low,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 10000,
        SupportsParallelization = false,
        RequiresIList = false
    };
    
    public PlatformCompatibility PlatformCompatibility => 
        PlatformCompatibility.WebAssembly | PlatformCompatibility.Browser;
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        // WASM prefers simple iteration patterns
        foreach (var item in source)
        {
            action(item);
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        // Use yield return to minimize memory allocation
        foreach (var item in source)
        {
            yield return transform(item);
        }
    }
    
    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        // WASM async is limited, use sequential processing
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
        // Use yield return for memory efficiency
        foreach (var item in source)
        {
            if (predicate(item))
            {
                yield return transform(item);
            }
        }
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return context.PlatformTarget switch
        {
            PlatformTarget.JavaScript => 
                GenerateJavaScriptCode(context),
            _ => new ForeachStrategy<T>().GenerateCode(context)
        };
    }
    
    private string GenerateJavaScriptCode(CodeGenerationContext context)
    {
        return $$"""
        // WebAssembly-optimized iteration
        for (const {{context.ItemName}} of {{context.CollectionName}}) {
            {{context.IterationBodyTemplate.Replace("{item}", context.ItemName)}}
        }
        """;
    }
}
