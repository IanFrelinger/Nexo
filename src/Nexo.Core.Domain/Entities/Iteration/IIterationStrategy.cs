using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.ValueObjects;

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
    /// Execute iteration with filtering and transformation
    /// </summary>
    IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector);
    
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
    bool CanHandle(IIterationPipelineContext context);
    
    /// <summary>
    /// Get priority score for this strategy in the given context
    /// </summary>
    int GetPriority(IIterationPipelineContext context);
    
    /// <summary>
    /// Estimate performance for given context
    /// </summary>
    Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context);
}

/// <summary>
/// Performance profile for iteration strategies
/// </summary>
public record IterationPerformanceProfile
{
    /// <summary>
    /// CPU efficiency level
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel CpuEfficiency { get; init; } = Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel.Medium;
    
    /// <summary>
    /// Memory efficiency level
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel MemoryEfficiency { get; init; } = Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel.Medium;
    
    /// <summary>
    /// Scalability level
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel Scalability { get; init; } = Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel.Medium;
    
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
    /// Collection name (alias for CollectionVariableName)
    /// </summary>
    public string CollectionName { get; init; } = "items";
    
    /// <summary>
    /// Variable name for the item
    /// </summary>
    public string ItemVariableName { get; init; } = "item";
    
    /// <summary>
    /// Item name (alias for ItemVariableName)
    /// </summary>
    public string ItemName { get; init; } = "item";
    
    /// <summary>
    /// Action to perform on each item
    /// </summary>
    public string ActionCode { get; init; } = "// Process item";
    
    /// <summary>
    /// Iteration body template (alias for ActionCode)
    /// </summary>
    public string IterationBodyTemplate { get; init; } = "// Process item";
    
    /// <summary>
    /// Whether to include null checks
    /// </summary>
    public bool IncludeNullChecks { get; init; } = true;
    
    /// <summary>
    /// Whether the context has a Where clause
    /// </summary>
    public bool HasWhere { get; init; } = false;
    
    /// <summary>
    /// Whether the context has a Select clause
    /// </summary>
    public bool HasSelect { get; init; } = false;
    
    /// <summary>
    /// Predicate template for Where clauses
    /// </summary>
    public string PredicateTemplate { get; init; } = "x => true";
    
    /// <summary>
    /// Transform template for Select clauses
    /// </summary>
    public string TransformTemplate { get; init; } = "x => x";
    
    /// <summary>
    /// Action template for ForEach operations
    /// </summary>
    public string ActionTemplate { get; init; } = "x => { /* action */ }";
    
    /// <summary>
    /// Whether to include bounds checking
    /// </summary>
    public bool IncludeBoundsChecking { get; init; } = true;
    
    /// <summary>
    /// Whether the context requires async processing
    /// </summary>
    public bool HasAsync { get; init; } = false;
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements PerformanceRequirements { get; init; } = new();
    
    /// <summary>
    /// Additional context for code generation
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
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
    /// Estimated data size (alias for DataSize)
    /// </summary>
    public int EstimatedDataSize => DataSize;
    
    /// <summary>
    /// Iteration requirements
    /// </summary>
    public IterationRequirements Requirements { get; init; } = new();
    
    /// <summary>
    /// Runtime environment profile
    /// </summary>
    public RuntimeEnvironmentProfile EnvironmentProfile { get; init; } = new();
    
    /// <summary>
    /// Pipeline context
    /// </summary>
    public IIterationPipelineContext? PipelineContext { get; init; }
    
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
    
    /// <summary>
    /// Code generation context
    /// </summary>
    public CodeGeneration? CodeGeneration { get; init; }
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

/// <summary>
/// Pipeline context interface for iteration strategies
/// </summary>
public interface IIterationPipelineContext
{
    /// <summary>
    /// Unique execution identifier
    /// </summary>
    string ExecutionId { get; }
    
    /// <summary>
    /// Execution start time
    /// </summary>
    DateTime StartTime { get; }
    
    /// <summary>
    /// Shared data store
    /// </summary>
    Dictionary<string, object> SharedData { get; }
    
    /// <summary>
    /// Get a value from shared data
    /// </summary>
    T? GetValue<T>(string key, T? defaultValue = default);
    
    /// <summary>
    /// Set a value in shared data
    /// </summary>
    void SetValue<T>(string key, T value);
    
    /// <summary>
    /// Check if a key exists in shared data
    /// </summary>
    bool HasValue(string key);
    
    /// <summary>
    /// Data size for iteration
    /// </summary>
    int DataSize { get; }
    
    /// <summary>
    /// Whether parallelization is required
    /// </summary>
    bool RequiresParallelization { get; }
    
    /// <summary>
    /// Platform target
    /// </summary>
    PlatformTarget PlatformTarget { get; }
    
    /// <summary>
    /// Priority level
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Iteration requirements for strategy selection
/// </summary>
public record IterationRequirements
{
    /// <summary>
    /// Whether to prioritize CPU efficiency
    /// </summary>
    public bool PrioritizeCpu { get; init; } = false;
    
    /// <summary>
    /// Whether to prioritize memory efficiency
    /// </summary>
    public bool PrioritizeMemory { get; init; } = false;
    
    /// <summary>
    /// Whether parallelization is required
    /// </summary>
    public bool RequiresParallelization { get; init; } = false;
    
    /// <summary>
    /// Whether ordering must be preserved
    /// </summary>
    public bool RequiresOrdering { get; init; } = false;
    
    /// <summary>
    /// Whether side effects are allowed
    /// </summary>
    public bool AllowSideEffects { get; init; } = true;
    
    /// <summary>
    /// Maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    
    /// <summary>
    /// Timeout for the operation
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Convert to PerformanceRequirements
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements ToPerformanceRequirements()
    {
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements
        {
            MaxExecutionTimeMs = (int)Timeout.TotalMilliseconds,
            MaxMemoryUsageMB = PrioritizeMemory ? 50 : 100,
            RequiresRealTime = PrioritizeCpu,
            PreferParallel = RequiresParallelization,
            MemoryCritical = PrioritizeMemory
        };
    }
}

/// <summary>
/// Extension methods for PerformanceRequirements
/// </summary>
public static class PerformanceRequirementsExtensions
{
    /// <summary>
    /// Convert PerformanceRequirements to IterationRequirements
    /// </summary>
    public static IterationRequirements ToIterationRequirements(this Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements performanceRequirements)
    {
        return new IterationRequirements
        {
            PrioritizeCpu = performanceRequirements.RequiresRealTime,
            PrioritizeMemory = performanceRequirements.MemoryCritical,
            RequiresParallelization = performanceRequirements.PreferParallel,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            Timeout = TimeSpan.FromMilliseconds(performanceRequirements.MaxExecutionTimeMs)
        };
    }
}

/// <summary>
/// Code generation context for iteration strategies
/// </summary>
public record CodeGeneration
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
    /// Whether the context has a Where clause
    /// </summary>
    public bool HasWhere { get; init; } = false;
    
    /// <summary>
    /// Whether the context has a Select clause
    /// </summary>
    public bool HasSelect { get; init; } = false;
    
    /// <summary>
    /// Predicate template for Where clauses
    /// </summary>
    public string PredicateTemplate { get; init; } = "x => true";
    
    /// <summary>
    /// Transform template for Select clauses
    /// </summary>
    public string TransformTemplate { get; init; } = "x => x";
    
    /// <summary>
    /// Action template for ForEach operations
    /// </summary>
    public string ActionTemplate { get; init; } = "x => { /* action */ }";
    
    /// <summary>
    /// Whether to include bounds checking
    /// </summary>
    public bool IncludeBoundsChecking { get; init; } = true;
    
    /// <summary>
    /// Whether the context requires async processing
    /// </summary>
    public bool HasAsync { get; init; } = false;
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements PerformanceRequirements { get; init; } = new();
    
    /// <summary>
    /// Additional context for code generation
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
}

/// <summary>
/// Iteration priority enumeration
/// </summary>
public enum IterationPriority
{
    Performance,
    Readability,
    Maintainability
}

/// <summary>
/// Platform target enumeration
/// </summary>
public enum PlatformTarget
{
    DotNet,
    Unity,
    Unity2022,
    Unity2023,
    WebAssembly,
    Mobile,
    Server,
    Browser,
    Native,
    JavaScript,
    Swift,
    Kotlin,
    Python,
    Java,
    Go,
    Rust,
    Cpp,
    Windows,
    Linux,
    macOS,
    iOS,
    Android,
    CSharp,
    Web
}
