using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for code generation optimization
/// </summary>
public interface ICodeGenerationOptimizer
{
    /// <summary>
    /// Optimize code generation for performance
    /// </summary>
    Task<CodeGenerationOptimizationResult> OptimizeForPerformanceAsync(CodeGenerationRequest request);
    
    /// <summary>
    /// Optimize code generation for quality
    /// </summary>
    Task<CodeGenerationOptimizationResult> OptimizeForQualityAsync(CodeGenerationRequest request);
    
    /// <summary>
    /// Optimize code generation for maintainability
    /// </summary>
    Task<CodeGenerationOptimizationResult> OptimizeForMaintainabilityAsync(CodeGenerationRequest request);
    
    /// <summary>
    /// Get optimization suggestions for code generation
    /// </summary>
    Task<IEnumerable<CodeGenerationOptimizationSuggestion>> GetOptimizationSuggestionsAsync(CodeGenerationRequest request);
    
    /// <summary>
    /// Analyze code generation quality
    /// </summary>
    Task<CodeGenerationQualityAnalysis> AnalyzeQualityAsync(string generatedCode);
}

/// <summary>
/// Code generation request
/// </summary>
public record CodeGenerationRequest
{
    /// <summary>
    /// Description of what to generate
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Target platform
    /// </summary>
    public string Platform { get; init; } = string.Empty;
    
    /// <summary>
    /// Programming language
    /// </summary>
    public string Language { get; init; } = string.Empty;
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
    
    /// <summary>
    /// Quality requirements
    /// </summary>
    public QualityRequirements QualityRequirements { get; init; } = new();
    
    /// <summary>
    /// Additional context
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Quality requirements
/// </summary>
public record QualityRequirements
{
    /// <summary>
    /// Minimum code quality score (0-100)
    /// </summary>
    public int MinimumQualityScore { get; init; } = 80;
    
    /// <summary>
    /// Maximum cyclomatic complexity
    /// </summary>
    public int MaxCyclomaticComplexity { get; init; } = 10;
    
    /// <summary>
    /// Maximum cognitive complexity
    /// </summary>
    public int MaxCognitiveComplexity { get; init; } = 15;
    
    /// <summary>
    /// Whether to include comprehensive error handling
    /// </summary>
    public bool IncludeErrorHandling { get; init; } = true;
    
    /// <summary>
    /// Whether to include comprehensive logging
    /// </summary>
    public bool IncludeLogging { get; init; } = true;
    
    /// <summary>
    /// Whether to include unit tests
    /// </summary>
    public bool IncludeUnitTests { get; init; } = false;
    
    /// <summary>
    /// Whether to include documentation
    /// </summary>
    public bool IncludeDocumentation { get; init; } = true;
}

/// <summary>
/// Code generation optimization result
/// </summary>
public record CodeGenerationOptimizationResult
{
    /// <summary>
    /// Optimized generated code
    /// </summary>
    public string OptimizedCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Optimization type applied
    /// </summary>
    public OptimizationType OptimizationType { get; init; }
    
    /// <summary>
    /// Quality score improvement
    /// </summary>
    public double QualityScoreImprovement { get; init; }
    
    /// <summary>
    /// Performance improvement percentage
    /// </summary>
    public double PerformanceImprovement { get; init; }
    
    /// <summary>
    /// Maintainability improvement percentage
    /// </summary>
    public double MaintainabilityImprovement { get; init; }
    
    /// <summary>
    /// Optimization suggestions applied
    /// </summary>
    public List<string> AppliedSuggestions { get; init; } = new();
    
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
/// Code generation optimization suggestion
/// </summary>
public record CodeGenerationOptimizationSuggestion
{
    /// <summary>
    /// Suggestion identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
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
    /// Implementation effort
    /// </summary>
    public ImplementationEffort Effort { get; init; }
    
    /// <summary>
    /// Code location (line number, etc.)
    /// </summary>
    public string Location { get; init; } = string.Empty;
}

/// <summary>
/// Code generation quality analysis
/// </summary>
public record CodeGenerationQualityAnalysis
{
    /// <summary>
    /// Overall quality score (0-100)
    /// </summary>
    public double OverallQualityScore { get; init; }
    
    /// <summary>
    /// Performance score (0-100)
    /// </summary>
    public double PerformanceScore { get; init; }
    
    /// <summary>
    /// Maintainability score (0-100)
    /// </summary>
    public double MaintainabilityScore { get; init; }
    
    /// <summary>
    /// Readability score (0-100)
    /// </summary>
    public double ReadabilityScore { get; init; }
    
    /// <summary>
    /// Security score (0-100)
    /// </summary>
    public double SecurityScore { get; init; }
    
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
    /// Quality issues identified
    /// </summary>
    public List<string> QualityIssues { get; init; } = new();
    
    /// <summary>
    /// Quality strengths
    /// </summary>
    public List<string> QualityStrengths { get; init; } = new();
    
    /// <summary>
    /// Recommendations for improvement
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
}