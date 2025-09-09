using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Service for generating optimized iteration code using AI
/// </summary>
public class IterationCodeGenerator : IIterationCodeGenerator
{
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<IterationCodeGenerator> _logger;

    public IterationCodeGenerator(
        IModelOrchestrator modelOrchestrator,
        ILogger<IterationCodeGenerator> logger)
    {
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateOptimalIterationCodeAsync(
        IterationContext context, 
        CodeGenerationContext codeGenerationContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildIterationPrompt(context, codeGenerationContext);
            
            var request = new ModelRequest
            {
                Input = prompt,
                SystemPrompt = "You are an expert code generator specializing in optimized iteration patterns. Generate clean, efficient, and platform-appropriate code.",
                Temperature = 0.3,
                MaxTokens = 2000,
                IncludeMetadata = true
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            
            if (!response.Success)
            {
                _logger.LogError("Failed to generate iteration code: {Error}", response.ErrorMessage);
                return GenerateFallbackCode(context, codeGenerationContext);
            }

            return response.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating iteration code");
            return GenerateFallbackCode(context, codeGenerationContext);
        }
    }

    public async Task<Dictionary<PlatformTarget, string>> GenerateMultiPlatformCodeAsync(
        IterationContext context,
        IEnumerable<PlatformTarget> platforms,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<PlatformTarget, string>();
        
        foreach (var platform in platforms)
        {
            var platformContext = context with { };
            var codeContext = new CodeGenerationContext
            {
                PlatformTarget = platform,
                CollectionName = "items",
                ItemName = "item",
                IterationBodyTemplate = "// {item}",
                ActionTemplate = "x => ProcessItem(x)",
                PredicateTemplate = "x => x != null",
                TransformTemplate = "x => x.ToString()",
                HasWhere = false,
                HasSelect = false,
                HasAsync = false
            };

            var code = await GenerateOptimalIterationCodeAsync(platformContext, codeContext, cancellationToken);
            results[platform] = code;
        }

        return results;
    }

    public async Task<string> EnhanceIterationCodeAsync(
        string existingCode,
        IterationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $$"""
                Analyze and enhance the following iteration code:
                
                ```csharp
                {{existingCode}}
                ```
                
                Context:
                - Data size: {context.DataSize}
                - Performance requirements: {(context.Requirements?.PrioritizeCpu == true ? "High" : "Medium")}
                - Platform: {context.EnvironmentProfile?.PlatformType ?? PlatformCompatibility.DotNet}
                
                Provide an enhanced version with:
                1. Better performance optimizations
                2. Improved readability
                3. Error handling
                4. Platform-specific optimizations
                """;

            var request = new ModelRequest
            {
                Input = prompt,
                SystemPrompt = "You are an expert code reviewer and optimizer. Enhance the provided code with best practices and optimizations.",
                Temperature = 0.2,
                MaxTokens = 3000,
                IncludeMetadata = true
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            
            if (!response.Success)
            {
                _logger.LogError("Failed to enhance iteration code: {Error}", response.ErrorMessage);
                return existingCode; // Return original code if enhancement fails
            }

            return response.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing iteration code");
            return existingCode; // Return original code if enhancement fails
        }
    }

    private string BuildIterationPrompt(IterationContext context, CodeGenerationContext codeContext)
    {
        var prompt = $"""
            Generate optimized iteration code with the following requirements:
            
            Platform: {codeContext.PlatformTarget}
            Collection: {codeContext.CollectionName}
            Item: {codeContext.ItemName}
            
            Context:
            - Data size: {context.DataSize}
            - Performance level: {(context.Requirements?.PrioritizeCpu == true ? "High" : "Medium")}
            - Platform compatibility: {context.EnvironmentProfile?.PlatformType ?? Nexo.Core.Domain.Entities.Infrastructure.PlatformType.DotNet}
            - CPU cores: {context.EnvironmentProfile?.CpuCores ?? 1}
            - Memory: {context.EnvironmentProfile?.AvailableMemoryMB ?? 0}MB
            
            Requirements:
            - Use {codeContext.ActionTemplate} for the action
            """;

        if (codeContext.HasWhere)
        {
            prompt += $"\n            - Filter with {codeContext.PredicateTemplate}";
        }

        if (codeContext.HasSelect)
        {
            prompt += $"\n            - Transform with {codeContext.TransformTemplate}";
        }

        if (codeContext.HasAsync)
        {
            prompt += "\n            - Use async/await patterns";
        }

        prompt += "\n            \n            Generate clean, efficient, and platform-appropriate code.";
        
        return prompt;
    }

    private string GenerateFallbackCode(IterationContext context, CodeGenerationContext codeContext)
    {
        // Simple fallback code generation
        return codeContext.PlatformTarget switch
        {
            PlatformTarget.CSharp => $$"""
                foreach (var {{codeContext.ItemName}} in {{codeContext.CollectionName}})
                {
                    {{codeContext.IterationBodyTemplate.Replace("{item}", codeContext.ItemName)}}
                }
                """,
            PlatformTarget.JavaScript => $$"""
                {{codeContext.CollectionName}}.forEach({{codeContext.ItemName}} => {
                    {{codeContext.IterationBodyTemplate.Replace("{item}", codeContext.ItemName)}}
                });
                """,
            _ => $$"""
                // Fallback iteration for {{codeContext.PlatformTarget}}
                foreach (var {{codeContext.ItemName}} in {{codeContext.CollectionName}})
                {
                    {{codeContext.IterationBodyTemplate.Replace("{item}", codeContext.ItemName)}}
                }
                """
        };
    }
}