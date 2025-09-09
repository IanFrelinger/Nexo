using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for code optimization
/// </summary>
public interface ICodeOptimizer
{
    /// <summary>
    /// Optimize code for performance
    /// </summary>
    Task<CodeOptimizationResult> OptimizeForPerformanceAsync(string code, OptimizationContext context);
    
    /// <summary>
    /// Optimize code for memory usage
    /// </summary>
    Task<CodeOptimizationResult> OptimizeForMemoryAsync(string code, OptimizationContext context);
    
    /// <summary>
    /// Optimize code for readability
    /// </summary>
    Task<CodeOptimizationResult> OptimizeForReadabilityAsync(string code, OptimizationContext context);
    
    /// <summary>
    /// Get optimization suggestions
    /// </summary>
    Task<IEnumerable<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(string code);
    
    /// <summary>
    /// Analyze code complexity
    /// </summary>
    Task<CodeComplexityAnalysis> AnalyzeComplexityAsync(string code);
    
    /// <summary>
    /// Set optimization level
    /// </summary>
    Task SetOptimizationLevelAsync(OptimizationLevel level);
}

/// <summary>
/// Code optimization result
/// </summary>
public record CodeOptimizationResult
{
    /// <summary>
    /// Optimized code
    /// </summary>
    public string OptimizedCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Optimization type applied
    /// </summary>
    public OptimizationType OptimizationType { get; init; }
    
    /// <summary>
    /// Performance improvement percentage
    /// </summary>
    public double PerformanceImprovement { get; init; }
    
    /// <summary>
    /// Memory improvement percentage
    /// </summary>
    public double MemoryImprovement { get; init; }
    
    /// <summary>
    /// Optimization suggestions
    /// </summary>
    public List<string> Suggestions { get; init; } = new();
    
    /// <summary>
    /// Whether optimization was successful
    /// </summary>
    public bool WasSuccessful { get; init; }
    
    /// <summary>
    /// Error message if optimization failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Optimization context
/// </summary>
public record OptimizationContext
{
    /// <summary>
    /// Target platform
    /// </summary>
    public string Platform { get; init; } = string.Empty;
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
    
    /// <summary>
    /// Optimization preferences
    /// </summary>
    public OptimizationPreferences Preferences { get; init; } = new();
    
    /// <summary>
    /// Additional context
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
}

/// <summary>
/// Optimization preferences
/// </summary>
public record OptimizationPreferences
{
    /// <summary>
    /// Whether to prioritize performance
    /// </summary>
    public bool PrioritizePerformance { get; init; } = true;
    
    /// <summary>
    /// Whether to prioritize memory usage
    /// </summary>
    public bool PrioritizeMemory { get; init; } = false;
    
    /// <summary>
    /// Whether to prioritize readability
    /// </summary>
    public bool PrioritizeReadability { get; init; } = false;
    
    /// <summary>
    /// Maximum optimization level (1-10)
    /// </summary>
    public int MaxOptimizationLevel { get; init; } = 5;
}

/// <summary>
/// Types of optimizations
/// </summary>
public enum OptimizationType
{
    /// <summary>
    /// Performance optimization
    /// </summary>
    Performance,
    
    /// <summary>
    /// Memory optimization
    /// </summary>
    Memory,
    
    /// <summary>
    /// Readability optimization
    /// </summary>
    Readability,
    
    /// <summary>
    /// Security optimization
    /// </summary>
    Security,
    
    /// <summary>
    /// General optimization
    /// </summary>
    General
}

/// <summary>
/// Optimization suggestion
/// </summary>
public record OptimizationSuggestion
{
    /// <summary>
    /// Suggestion description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Suggestion type
    /// </summary>
    public OptimizationType Type { get; init; }
    
    /// <summary>
    /// Priority level (1-10)
    /// </summary>
    public int Priority { get; init; }
    
    /// <summary>
    /// Expected improvement percentage
    /// </summary>
    public double ExpectedImprovement { get; init; }
    
    /// <summary>
    /// Code location (line number, etc.)
    /// </summary>
    public string Location { get; init; } = string.Empty;
}

/// <summary>
/// Code complexity analysis
/// </summary>
public record CodeComplexityAnalysis
{
    /// <summary>
    /// Cyclomatic complexity
    /// </summary>
    public int CyclomaticComplexity { get; init; }
    
    /// <summary>
    /// Cognitive complexity
    /// </summary>
    public int CognitiveComplexity { get; init; }
    
    /// <summary>
    /// Lines of code
    /// </summary>
    public int LinesOfCode { get; init; }
    
    /// <summary>
    /// Number of methods
    /// </summary>
    public int MethodCount { get; init; }
    
    /// <summary>
    /// Number of classes
    /// </summary>
    public int ClassCount { get; init; }
    
    /// <summary>
    /// Complexity rating
    /// </summary>
    public ComplexityRating Rating { get; init; }
}

/// <summary>
/// Complexity ratings
/// </summary>
public enum ComplexityRating
{
    /// <summary>
    /// Low complexity
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium complexity
    /// </summary>
    Medium,
    
    /// <summary>
    /// High complexity
    /// </summary>
    High,
    
    /// <summary>
    /// Very high complexity
    /// </summary>
    VeryHigh
}