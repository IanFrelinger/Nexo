using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Agents;

/// <summary>
/// AI agent specialized in iteration optimization and strategy selection
/// </summary>
public class IterationOptimizationAgent : IAIAgent
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<IterationOptimizationAgent> _logger;
    
    public string AgentId => "IterationOptimization";
    public AgentCapabilities Capabilities => AgentCapabilities.CodeGeneration | AgentCapabilities.PerformanceAnalysis;
    
    public IterationOptimizationAgent(
        IIterationStrategySelector strategySelector,
        IModelOrchestrator modelOrchestrator,
        ILogger<IterationOptimizationAgent> logger)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AgentResponse> ProcessAsync(AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing iteration optimization request");
            
            // Analyze the code generation requirements for iteration patterns
            var iterationAnalysis = await AnalyzeIterationRequirements(request.Input);
            
            if (iterationAnalysis.RequiresIteration)
            {
                // Select optimal strategy
                var strategy = _strategySelector.SelectStrategy<object>(iterationAnalysis.IterationContext);
                
                // Generate optimized iteration code
                var codeGenerationContext = new CodeGenerationContext
                {
                    PlatformTarget = iterationAnalysis.TargetPlatform,
                    CollectionVariableName = iterationAnalysis.CollectionVariableName,
                    ItemVariableName = iterationAnalysis.ItemVariableName,
                    ActionCode = iterationAnalysis.ActionCode,
                    IncludeNullChecks = iterationAnalysis.IncludeNullChecks,
                    IncludeBoundsChecking = iterationAnalysis.IncludeBoundsChecking,
                    PerformanceRequirements = iterationAnalysis.PerformanceRequirements,
                    AdditionalContext = iterationAnalysis.AdditionalContext
                };
                
                var optimizedCode = strategy.GenerateCode(codeGenerationContext);
                
                // Enhance with AI if needed
                if (iterationAnalysis.RequiresComplexLogic)
                {
                    var enhancementPrompt = CreateIterationEnhancementPrompt(optimizedCode, iterationAnalysis);
                    var enhanced = await _modelOrchestrator.ProcessAsync(enhancementPrompt);
                    optimizedCode = enhanced.Response;
                }
                
                _logger.LogInformation("Generated optimized iteration code using strategy {StrategyId}", strategy.StrategyId);
                
                return new AgentResponse
                {
                    Result = optimizedCode,
                    Confidence = CalculateConfidence(iterationAnalysis, strategy),
                    Metadata = new Dictionary<string, object>
                    {
                        ["IterationStrategy"] = strategy.StrategyId,
                        ["PerformanceProfile"] = strategy.PerformanceProfile,
                        ["PlatformOptimized"] = true,
                        ["IterationAnalysis"] = iterationAnalysis,
                        ["PerformanceEstimate"] = strategy.EstimatePerformance(iterationAnalysis.IterationContext)
                    }
                };
            }
            
            return AgentResponse.NoAction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing iteration optimization request");
            return new AgentResponse
            {
                Result = string.Empty,
                Confidence = 0.0,
                ErrorMessage = ex.Message
            };
        }
    }
    
    private async Task<IterationAnalysis> AnalyzeIterationRequirements(string input)
    {
        var analysisPrompt = $"""
        Analyze this code generation request for iteration requirements:
        
        {input}
        
        Determine:
        1. Does this require iteration over collections?
        2. What is the estimated data size?
        3. What are the performance requirements?
        4. What platform is the target?
        5. Does it need parallel processing?
        6. What type of operation is being performed (CPU-bound, I/O-bound)?
        7. What are the variable names for collection and items?
        8. What action needs to be performed on each item?
        
        Respond with structured analysis in the following format:
        - RequiresIteration: true/false
        - EstimatedDataSize: number
        - TargetPlatform: platform name
        - PerformanceRequirements: requirements
        - IsCpuBound: true/false
        - IsIoBound: true/false
        - RequiresAsync: true/false
        - CollectionVariableName: variable name
        - ItemVariableName: variable name
        - ActionCode: action description
        - RequiresComplexLogic: true/false
        """;
        
        var response = await _modelOrchestrator.ProcessAsync(analysisPrompt);
        return ParseIterationAnalysis(response.Response);
    }
    
    private IterationAnalysis ParseIterationAnalysis(string response)
    {
        // Simple parsing - in a real implementation, this would use more sophisticated parsing
        var analysis = new IterationAnalysis
        {
            RequiresIteration = response.Contains("RequiresIteration: true"),
            EstimatedDataSize = ExtractNumber(response, "EstimatedDataSize") ?? 1000,
            TargetPlatform = ExtractPlatform(response),
            PerformanceRequirements = ExtractPerformanceRequirements(response),
            IsCpuBound = response.Contains("IsCpuBound: true"),
            IsIoBound = response.Contains("IsIoBound: true"),
            RequiresAsync = response.Contains("RequiresAsync: true"),
            CollectionVariableName = ExtractString(response, "CollectionVariableName") ?? "items",
            ItemVariableName = ExtractString(response, "ItemVariableName") ?? "item",
            ActionCode = ExtractString(response, "ActionCode") ?? "// Process item",
            RequiresComplexLogic = response.Contains("RequiresComplexLogic: true"),
            IncludeNullChecks = true,
            IncludeBoundsChecking = true,
            AdditionalContext = new Dictionary<string, object>()
        };
        
        // Create iteration context
        analysis.IterationContext = new IterationContext
        {
            DataSize = analysis.EstimatedDataSize,
            Requirements = analysis.PerformanceRequirements,
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = analysis.TargetPlatform,
            IsCpuBound = analysis.IsCpuBound,
            IsIoBound = analysis.IsIoBound,
            RequiresAsync = analysis.RequiresAsync
        };
        
        return analysis;
    }
    
    private int? ExtractNumber(string text, string key)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains(key + ":"))
            {
                var parts = line.Split(':');
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var number))
                {
                    return number;
                }
            }
        }
        return null;
    }
    
    private string? ExtractString(string text, string key)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains(key + ":"))
            {
                var parts = line.Split(':');
                if (parts.Length > 1)
                {
                    return parts[1].Trim();
                }
            }
        }
        return null;
    }
    
    private PlatformTarget ExtractPlatform(string response)
    {
        if (response.Contains("Unity"))
            return PlatformTarget.Unity2023;
        if (response.Contains("JavaScript"))
            return PlatformTarget.JavaScript;
        if (response.Contains("Swift"))
            return PlatformTarget.Swift;
        if (response.Contains("Kotlin"))
            return PlatformTarget.Kotlin;
        if (response.Contains("WebAssembly"))
            return PlatformTarget.WebAssembly;
        
        return PlatformTarget.DotNet;
    }
    
    private PerformanceRequirements ExtractPerformanceRequirements(string response)
    {
        var requirements = new PerformanceRequirements();
        
        if (response.Contains("real-time") || response.Contains("Real-time"))
        {
            requirements = requirements with { RequiresRealTime = true };
        }
        
        if (response.Contains("parallel") || response.Contains("Parallel"))
        {
            requirements = requirements with { PreferParallel = true };
        }
        
        if (response.Contains("memory") || response.Contains("Memory"))
        {
            requirements = requirements with { MemoryCritical = true };
        }
        
        return requirements;
    }
    
    private string CreateIterationEnhancementPrompt(string optimizedCode, IterationAnalysis analysis)
    {
        return $"""
        Enhance this optimized iteration code with additional improvements:
        
        Original Code:
        {optimizedCode}
        
        Context:
        - Platform: {analysis.TargetPlatform}
        - Data Size: {analysis.EstimatedDataSize}
        - Performance Requirements: {analysis.PerformanceRequirements}
        - CPU Bound: {analysis.IsCpuBound}
        - I/O Bound: {analysis.IsIoBound}
        - Requires Async: {analysis.RequiresAsync}
        
        Please enhance the code with:
        1. Additional performance optimizations
        2. Error handling
        3. Logging if appropriate
        4. Platform-specific optimizations
        5. Best practices for the target platform
        
        Return only the enhanced code without explanations.
        """;
    }
    
    private double CalculateConfidence(IterationAnalysis analysis, IIterationStrategy<object> strategy)
    {
        var confidence = 0.8; // Base confidence
        
        // Increase confidence based on strategy suitability
        var performanceEstimate = strategy.EstimatePerformance(analysis.IterationContext);
        if (performanceEstimate.MeetsRequirements)
        {
            confidence += 0.1;
        }
        
        // Increase confidence for well-defined requirements
        if (analysis.EstimatedDataSize > 0)
        {
            confidence += 0.05;
        }
        
        if (!string.IsNullOrEmpty(analysis.ActionCode))
        {
            confidence += 0.05;
        }
        
        return Math.Min(1.0, confidence);
    }
}

/// <summary>
/// Analysis of iteration requirements
/// </summary>
public record IterationAnalysis
{
    /// <summary>
    /// Whether iteration is required
    /// </summary>
    public bool RequiresIteration { get; init; }
    
    /// <summary>
    /// Estimated data size
    /// </summary>
    public int EstimatedDataSize { get; init; }
    
    /// <summary>
    /// Target platform
    /// </summary>
    public PlatformTarget TargetPlatform { get; init; }
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
    
    /// <summary>
    /// Whether operation is CPU-bound
    /// </summary>
    public bool IsCpuBound { get; init; }
    
    /// <summary>
    /// Whether operation is I/O-bound
    /// </summary>
    public bool IsIoBound { get; init; }
    
    /// <summary>
    /// Whether async processing is required
    /// </summary>
    public bool RequiresAsync { get; init; }
    
    /// <summary>
    /// Collection variable name
    /// </summary>
    public string CollectionVariableName { get; init; } = string.Empty;
    
    /// <summary>
    /// Item variable name
    /// </summary>
    public string ItemVariableName { get; init; } = string.Empty;
    
    /// <summary>
    /// Action code
    /// </summary>
    public string ActionCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether complex logic is required
    /// </summary>
    public bool RequiresComplexLogic { get; init; }
    
    /// <summary>
    /// Whether to include null checks
    /// </summary>
    public bool IncludeNullChecks { get; init; }
    
    /// <summary>
    /// Whether to include bounds checking
    /// </summary>
    public bool IncludeBoundsChecking { get; init; }
    
    /// <summary>
    /// Additional context
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
    
    /// <summary>
    /// Iteration context
    /// </summary>
    public IterationContext? IterationContext { get; init; }
}
