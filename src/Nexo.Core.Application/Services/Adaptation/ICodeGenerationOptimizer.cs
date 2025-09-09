using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Interface for optimizing code generation
/// </summary>
public interface ICodeGenerationOptimizer
{
    /// <summary>
    /// Optimize code generation
    /// </summary>
    Task<CodeOptimizationResult> OptimizeCodeGenerationAsync(string code, OptimizationContext context);
    
    /// <summary>
    /// Get optimization suggestions
    /// </summary>
    Task<IEnumerable<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(string code);
    
    /// <summary>
    /// Analyze code complexity
    /// </summary>
    Task<CodeComplexityAnalysis> AnalyzeCodeComplexityAsync(string code);
    
    /// <summary>
    /// Get performance recommendations
    /// </summary>
    Task<IEnumerable<string>> GetPerformanceRecommendationsAsync(string code);
    
    /// <summary>
    /// Optimize for platform
    /// </summary>
    Task<string> OptimizeForPlatformAsync(string code, PlatformType platform);
    
    /// <summary>
    /// Get code generation metrics
    /// </summary>
    Task<CodeGenerationMetrics> GetCodeGenerationMetricsAsync();
    
    /// <summary>
    /// Validate generated code
    /// </summary>
    Task<CodeValidationResult> ValidateGeneratedCodeAsync(string code);
    
    /// <summary>
    /// Get code generation insights
    /// </summary>
    Task<IEnumerable<LearningInsight>> GetCodeGenerationInsightsAsync();
}
