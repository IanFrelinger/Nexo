using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Domain.Entities.Iteration;

/// <summary>
/// Core abstraction for iteration strategies that can be swapped at runtime
/// </summary>
public interface IIterationStrategy<T>
{
    /// <summary>
    /// Strategy identifier for selection and caching
    /// </summary>
    string StrategyId { get; }
    
    /// <summary>
    /// Performance characteristics of this strategy
    /// </summary>
    IterationPerformanceProfile PerformanceProfile { get; }
    
    /// <summary>
    /// Platforms this strategy is optimized for
    /// </summary>
    PlatformCompatibility PlatformCompatibility { get; }
    
    /// <summary>
    /// Execute iteration with the given action
    /// </summary>
    void Execute(IEnumerable<T> source, Action<T> action);
    
    /// <summary>
    /// Execute iteration with transformation
    /// </summary>
    IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> transform);
    
    /// <summary>
    /// Execute async iteration
    /// </summary>
    Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction);
    
    /// <summary>
    /// Execute with filtering and transformation
    /// </summary>
    IEnumerable<TResult> ExecuteWhere<TResult>(
        IEnumerable<T> source, 
        Func<T, bool> predicate, 
        Func<T, TResult> transform);
    
    /// <summary>
    /// Generate code representation of this iteration for AI code generation
    /// </summary>
    string GenerateCode(CodeGenerationContext context);
}

/// <summary>
/// Performance characteristics for strategy selection
/// </summary>
public record IterationPerformanceProfile
{
    public PerformanceLevel CpuEfficiency { get; init; }
    public PerformanceLevel MemoryEfficiency { get; init; }
    public PerformanceLevel Scalability { get; init; }
    public int OptimalDataSizeMin { get; init; }
    public int OptimalDataSizeMax { get; init; }
    public bool SupportsParallelization { get; init; }
    public bool RequiresIList { get; init; }
}

public enum PerformanceLevel { Low, Medium, High, Excellent }

/// <summary>
/// Platform compatibility flags
/// </summary>
[Flags]
public enum PlatformCompatibility
{
    None = 0,
    DotNet = 1,
    Unity = 2,
    WebAssembly = 4,
    Mobile = 8,
    Server = 16,
    Browser = 32,
    All = DotNet | Unity | WebAssembly | Mobile | Server | Browser
}

/// <summary>
/// Context for AI code generation
/// </summary>
public record CodeGenerationContext
{
    public PlatformTarget PlatformTarget { get; init; }
    public string CollectionName { get; init; } = "items";
    public string ItemName { get; init; } = "item";
    public string IterationBodyTemplate { get; init; } = "// {item}";
    public string ActionTemplate { get; init; } = "x => ProcessItem(x)";
    public string PredicateTemplate { get; init; } = "x => x != null";
    public string TransformTemplate { get; init; } = "x => x.ToString()";
    public bool HasWhere { get; init; } = false;
    public bool HasSelect { get; init; } = false;
    public bool HasAsync { get; init; } = false;
}

public enum PlatformTarget
{
    CSharp,
    Unity2022,
    Unity2023,
    JavaScript,
    Swift,
    Python,
    Java
}
