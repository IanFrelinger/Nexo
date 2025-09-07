using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// AI-powered code generator that selects optimal iteration strategies
/// </summary>
public class IterationCodeGenerator : IIterationCodeGenerator
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly IModelOrchestrator _aiOrchestrator;
    private readonly ILogger<IterationCodeGenerator> _logger;
    
    public IterationCodeGenerator(
        IIterationStrategySelector strategySelector,
        IModelOrchestrator aiOrchestrator,
        ILogger<IterationCodeGenerator> logger)
    {
        _strategySelector = strategySelector;
        _aiOrchestrator = aiOrchestrator;
        _logger = logger;
    }
    
    public async Task<string> GenerateOptimalIterationCodeAsync(IterationCodeRequest request)
    {
        try
        {
            // Select optimal strategy based on context
            var strategy = _strategySelector.SelectStrategy<object>(request.Context);
            
            // Generate base code using strategy
            var baseCode = strategy.GenerateCode(request.CodeGeneration);
            
            // Enhance with AI if requested
            if (request.UseAIEnhancement)
            {
                var enhancementPrompt = CreateEnhancementPrompt(baseCode, request);
                var enhancedCode = await _aiOrchestrator.ProcessAsync(enhancementPrompt);
                return enhancedCode.Response;
            }
            
            return baseCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating iteration code for request {Request}", request);
            return GenerateFallbackCode(request);
        }
    }
    
    public async Task<IEnumerable<string>> GenerateMultiplePlatformIterationsAsync(IterationCodeRequest request)
    {
        var results = new List<string>();
        
        foreach (var platform in request.TargetPlatforms)
        {
            var platformContext = request.CodeGeneration with { PlatformTarget = platform };
            var platformRequest = request with { CodeGeneration = platformContext };
            
            var code = await GenerateOptimalIterationCodeAsync(platformRequest);
            results.Add($"// {platform}\n{code}");
        }
        
        return results;
    }
    
    private string CreateEnhancementPrompt(string baseCode, IterationCodeRequest request)
    {
        return $"""
        Enhance this iteration code for {request.CodeGeneration.PlatformTarget}:
        
        Base Code:
        {baseCode}
        
        Requirements:
        - Performance: {(request.Context.Requirements.PrioritizeCpu ? "CPU-optimized" : "Balanced")}
        - Memory: {(request.Context.Requirements.PrioritizeMemory ? "Memory-optimized" : "Balanced")}
        - Platform: {request.CodeGeneration.PlatformTarget}
        - Data Size: {request.Context.DataSize} items
        - Collection: {request.CodeGeneration.CollectionName}
        - Action: {request.CodeGeneration.IterationBodyTemplate}
        
        Please optimize for the platform and add appropriate error handling, null checks, and performance optimizations.
        Return only the enhanced code without explanations.
        """;
    }
    
    private string GenerateFallbackCode(IterationCodeRequest request)
    {
        return request.CodeGeneration.PlatformTarget switch
        {
            PlatformTarget.JavaScript => 
                $"{request.CodeGeneration.CollectionName}.forEach({request.CodeGeneration.ActionTemplate})",
            PlatformTarget.Python => 
                $"for {request.CodeGeneration.ItemName} in {request.CodeGeneration.CollectionName}:\n    {request.CodeGeneration.IterationBodyTemplate.Replace("{item}", request.CodeGeneration.ItemName)}",
            PlatformTarget.Swift => 
                $"for {request.CodeGeneration.ItemName} in {request.CodeGeneration.CollectionName} {{\n    {request.CodeGeneration.IterationBodyTemplate.Replace("{item}", request.CodeGeneration.ItemName)}\n}}",
            _ => 
                $"foreach (var {request.CodeGeneration.ItemName} in {request.CodeGeneration.CollectionName})\n{{\n    {request.CodeGeneration.IterationBodyTemplate.Replace("{item}", request.CodeGeneration.ItemName)}\n}}"
        };
    }
}

/// <summary>
/// AI model orchestrator interface for code generation
/// </summary>
public interface IModelOrchestrator
{
    Task<ModelResponse> ProcessAsync(string prompt);
}

/// <summary>
/// Model response from AI orchestrator
/// </summary>
public record ModelResponse
{
    public string Response { get; init; } = "";
    public bool Success { get; init; }
    public string? Error { get; init; }
}
