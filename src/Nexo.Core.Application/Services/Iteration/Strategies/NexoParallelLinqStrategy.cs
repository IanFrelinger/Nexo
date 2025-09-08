using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Core.Application.Services.Iteration.Strategies;

/// <summary>
/// Parallel LINQ iteration strategy for CPU-intensive operations and large datasets
/// </summary>
public class NexoParallelLinqStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "Nexo.ParallelLinq";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Excellent,
        MemoryEfficiency = PerformanceLevel.Medium,
        Scalability = PerformanceLevel.Excellent,
        OptimalDataSizeMin = 1000,
        OptimalDataSizeMax = int.MaxValue,
        SupportsParallelization = true,
        RequiresIList = false,
        SupportsAsync = true,
        SuitableForRealTime = false
    };
    
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.DotNet | PlatformCompatibility.Server;
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        // Parallel LINQ for action execution
        source.AsParallel().ForAll(action);
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform)
    {
        return source.AsParallel().Select(transform);
    }
    
    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        // Parallel async execution
        var tasks = source.Select(asyncAction);
        await Task.WhenAll(tasks);
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return context.PlatformTarget switch
        {
            PlatformTarget.DotNet => GenerateCSharpParallelLinq(context),
            PlatformTarget.Server => GenerateServerParallelLinq(context),
            _ => GenerateCSharpParallelLinq(context) // Default to C# implementation
        };
    }
    
    public bool CanHandle(PipelineContext context)
    {
        var currentPlatform = context.GetPlatformTarget();
        var dataSize = EstimateDataSize(context);
        var cpuCores = Environment.ProcessorCount;
        
        // Only suitable for .NET platforms with multiple cores and sufficient data
        return PlatformCompatibility.HasFlag(GetPlatformFlag(currentPlatform)) &&
               cpuCores > 1 &&
               dataSize >= 1000;
    }
    
    public int GetPriority(PipelineContext context)
    {
        var dataSize = EstimateDataSize(context);
        var platform = context.GetPlatformTarget();
        var requirements = context.GetPerformanceRequirements();
        var cpuCores = Environment.ProcessorCount;
        
        // High priority for CPU-bound operations with large datasets
        if (requirements.PreferParallel && dataSize > 10000 && cpuCores > 2) return 95;
        if (context.IsCpuBound() && dataSize > 5000 && cpuCores > 1) return 90;
        if (dataSize > 50000 && cpuCores > 2) return 85;
        if (requirements.RequiresRealTime) return 20; // Not suitable for real-time
        
        return 50; // Default priority
    }
    
    public PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var dataSize = context.DataSize;
        var platform = context.TargetPlatform;
        var cpuCores = context.EnvironmentProfile.CpuCores;
        var isCpuBound = context.IsCpuBound;
        
        // Parallel LINQ performance depends on CPU cores and data size
        var baseTimePerItem = 0.003; // 3 microseconds per item (single-threaded)
        
        // Calculate parallel speedup (diminishing returns with more cores)
        var parallelSpeedup = Math.Min(cpuCores, 8) * 0.7; // Max 8 cores, 70% efficiency
        
        // Adjust for CPU-bound vs I/O-bound operations
        var cpuBoundMultiplier = isCpuBound ? 1.0 : 0.3; // I/O-bound operations don't benefit as much
        
        var estimatedTime = (dataSize * baseTimePerItem) / (parallelSpeedup * cpuBoundMultiplier);
        var estimatedMemory = dataSize * 0.02; // Higher memory overhead due to parallelization
        
        return new PerformanceEstimate
        {
            EstimatedExecutionTimeMs = estimatedTime,
            EstimatedMemoryUsageMB = estimatedMemory,
            Confidence = 0.85,
            PerformanceScore = CalculatePerformanceScore(estimatedTime, estimatedMemory, context),
            MeetsRequirements = estimatedTime <= context.Requirements.MaxExecutionTimeMs &&
                               estimatedMemory <= context.Requirements.MaxMemoryUsageMB
        };
    }
    
    private string GenerateCSharpParallelLinq(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        var nullCheck = context.IncludeNullChecks ? $"if ({collectionVar} != null)" : "";
        
        return $@"{nullCheck}
{{
    // Parallel LINQ - excellent for CPU-intensive operations
    var results = {collectionVar}
        .AsParallel()
        .WithDegreeOfParallelism(Environment.ProcessorCount)
        .Select({itemVar} => {{
            {action}
            return {itemVar};
        }})
        .ToList();
}}";
    }
    
    private string GenerateServerParallelLinq(CodeGenerationContext context)
    {
        var collectionVar = context.CollectionVariableName;
        var itemVar = context.ItemVariableName;
        var action = context.ActionCode;
        
        return $@"// Server-optimized Parallel LINQ
if ({collectionVar} != null)
{{
    var results = {collectionVar}
        .AsParallel()
        .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, 16))
        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
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
            PlatformTarget.Server => PlatformCompatibility.Server,
            _ => PlatformCompatibility.None
        };
    }
    
    private int EstimateDataSize(PipelineContext context)
    {
        if (context.TryGetProperty("EstimatedDataSize", out var size) && size is int dataSize)
        {
            return dataSize;
        }
        
        return 1000;
    }
    
    private double CalculatePerformanceScore(double executionTime, double memoryUsage, IterationContext context)
    {
        var timeScore = Math.Max(0, 100 - (executionTime / context.DataSize) * 1000);
        var memoryScore = Math.Max(0, 100 - memoryUsage * 10);
        
        return (timeScore + memoryScore) / 2;
    }
}
