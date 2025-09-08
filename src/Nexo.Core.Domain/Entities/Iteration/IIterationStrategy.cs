using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Core.Domain.Entities.Iteration;

/// <summary>
/// Core iteration strategy abstraction for the Nexo pipeline
/// </summary>
public interface IIterationStrategy<T>
{
    /// <summary>
    /// Unique identifier for this iteration strategy
    /// </summary>
    string StrategyId { get; }
    
    /// <summary>
    /// Performance characteristics of this strategy
    /// </summary>
    IterationPerformanceProfile PerformanceProfile { get; }
    
    /// <summary>
    /// Platform compatibility for this strategy
    /// </summary>
    PlatformCompatibility PlatformCompatibility { get; }
    
    /// <summary>
    /// Execute iteration with action
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
    /// Generate code for Feature Factory
    /// </summary>
    string GenerateCode(CodeGenerationContext context);
    
    /// <summary>
    /// Check if this strategy can handle the given pipeline context
    /// </summary>
    bool CanHandle(PipelineContext context);
    
    /// <summary>
    /// Get priority score for this strategy in the given context
    /// </summary>
    int GetPriority(PipelineContext context);
    
    /// <summary>
    /// Estimate performance for given context
    /// </summary>
    PerformanceEstimate EstimatePerformance(IterationContext context);
}

/// <summary>
/// Performance profile for iteration strategies
/// </summary>
public record IterationPerformanceProfile
{
    /// <summary>
    /// CPU efficiency level
    /// </summary>
    public PerformanceLevel CpuEfficiency { get; init; } = PerformanceLevel.Medium;
    
    /// <summary>
    /// Memory efficiency level
    /// </summary>
    public PerformanceLevel MemoryEfficiency { get; init; } = PerformanceLevel.Medium;
    
    /// <summary>
    /// Scalability level
    /// </summary>
    public PerformanceLevel Scalability { get; init; } = PerformanceLevel.Medium;
    
    /// <summary>
    /// Minimum optimal data size
    /// </summary>
    public int OptimalDataSizeMin { get; init; } = 0;
    
    /// <summary>
    /// Maximum optimal data size
    /// </summary>
    public int OptimalDataSizeMax { get; init; } = int.MaxValue;
    
    /// <summary>
    /// Whether this strategy supports parallelization
    /// </summary>
    public bool SupportsParallelization { get; init; } = false;
    
    /// <summary>
    /// Whether this strategy requires IList interface
    /// </summary>
    public bool RequiresIList { get; init; } = false;
    
    /// <summary>
    /// Whether this strategy supports async operations
    /// </summary>
    public bool SupportsAsync { get; init; } = false;
    
    /// <summary>
    /// Whether this strategy is suitable for real-time scenarios
    /// </summary>
    public bool SuitableForRealTime { get; init; } = false;
}

/// <summary>
/// Performance levels for iteration strategies
/// </summary>
public enum PerformanceLevel
{
    Low,
    Medium,
    High,
    Excellent
}

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
    Native = 64,
    All = DotNet | Unity | WebAssembly | Mobile | Server | Browser | Native
}

/// <summary>
/// Code generation context for iteration strategies
/// </summary>
public record CodeGenerationContext
{
    /// <summary>
    /// Target platform for code generation
    /// </summary>
    public PlatformTarget PlatformTarget { get; init; } = PlatformTarget.DotNet;
    
    /// <summary>
    /// Variable name for the collection
    /// </summary>
    public string CollectionVariableName { get; init; } = "items";
    
    /// <summary>
    /// Variable name for the item
    /// </summary>
    public string ItemVariableName { get; init; } = "item";
    
    /// <summary>
    /// Action to perform on each item
    /// </summary>
    public string ActionCode { get; init; } = "// Process item";
    
    /// <summary>
    /// Whether to include null checks
    /// </summary>
    public bool IncludeNullChecks { get; init; } = true;
    
    /// <summary>
    /// Whether to include bounds checking
    /// </summary>
    public bool IncludeBoundsChecking { get; init; } = true;
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
    
    /// <summary>
    /// Additional context for code generation
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
}

/// <summary>
/// Performance requirements for iteration
/// </summary>
public record PerformanceRequirements
{
    /// <summary>
    /// Maximum acceptable execution time in milliseconds
    /// </summary>
    public int MaxExecutionTimeMs { get; init; } = 1000;
    
    /// <summary>
    /// Maximum memory usage in MB
    /// </summary>
    public int MaxMemoryUsageMB { get; init; } = 100;
    
    /// <summary>
    /// Whether real-time performance is required
    /// </summary>
    public bool RequiresRealTime { get; init; } = false;
    
    /// <summary>
    /// Whether parallel processing is preferred
    /// </summary>
    public bool PreferParallel { get; init; } = false;
    
    /// <summary>
    /// Whether memory efficiency is critical
    /// </summary>
    public bool MemoryCritical { get; init; } = false;
}

/// <summary>
/// Iteration context for strategy selection
/// </summary>
public record IterationContext
{
    /// <summary>
    /// Estimated data size
    /// </summary>
    public int DataSize { get; init; }
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public PerformanceRequirements Requirements { get; init; } = new();
    
    /// <summary>
    /// Runtime environment profile
    /// </summary>
    public RuntimeEnvironmentProfile EnvironmentProfile { get; init; } = new();
    
    /// <summary>
    /// Pipeline context
    /// </summary>
    public PipelineContext? PipelineContext { get; init; }
    
    /// <summary>
    /// Target platform
    /// </summary>
    public PlatformTarget TargetPlatform { get; init; } = PlatformTarget.DotNet;
    
    /// <summary>
    /// Whether the operation is CPU-bound
    /// </summary>
    public bool IsCpuBound { get; init; } = false;
    
    /// <summary>
    /// Whether the operation is I/O-bound
    /// </summary>
    public bool IsIoBound { get; init; } = false;
    
    /// <summary>
    /// Whether the operation requires async processing
    /// </summary>
    public bool RequiresAsync { get; init; } = false;
}

/// <summary>
/// Runtime environment profile
/// </summary>
public record RuntimeEnvironmentProfile
{
    /// <summary>
    /// Platform type
    /// </summary>
    public PlatformType PlatformType { get; init; } = PlatformType.DotNet;
    
    /// <summary>
    /// Number of CPU cores
    /// </summary>
    public int CpuCores { get; init; } = Environment.ProcessorCount;
    
    /// <summary>
    /// Available memory in MB
    /// </summary>
    public long AvailableMemoryMB { get; init; } = GC.GetTotalMemory(false) / 1024 / 1024;
    
    /// <summary>
    /// Whether running in a constrained environment
    /// </summary>
    public bool IsConstrained { get; init; } = false;
    
    /// <summary>
    /// Whether running in a mobile environment
    /// </summary>
    public bool IsMobile { get; init; } = false;
    
    /// <summary>
    /// Whether running in a web environment
    /// </summary>
    public bool IsWeb { get; init; } = false;
    
    /// <summary>
    /// Whether running in Unity
    /// </summary>
    public bool IsUnity { get; init; } = false;
}

/// <summary>
/// Platform types
/// </summary>
public enum PlatformType
{
    DotNet,
    Unity,
    WebAssembly,
    JavaScript,
    Swift,
    Kotlin,
    Native,
    Mobile,
    Web,
    Server
}

/// <summary>
/// Performance estimate for iteration strategy
/// </summary>
public record PerformanceEstimate
{
    /// <summary>
    /// Estimated execution time in milliseconds
    /// </summary>
    public double EstimatedExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Estimated memory usage in MB
    /// </summary>
    public double EstimatedMemoryUsageMB { get; init; }
    
    /// <summary>
    /// Confidence level of the estimate (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Performance score (higher is better)
    /// </summary>
    public double PerformanceScore { get; init; }
    
    /// <summary>
    /// Whether this strategy meets the requirements
    /// </summary>
    public bool MeetsRequirements { get; init; }
}