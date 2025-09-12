using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Results;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI-powered documentation generation pipeline step for automatic documentation
    /// </summary>
    public class AIDocumentationStep : IPipelineStep<DocumentationRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AIDocumentationStep> _logger;

        public AIDocumentationStep(IAIRuntimeSelector runtimeSelector, ILogger<AIDocumentationStep> logger)
        {
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "AI Documentation Generation";
        public int Order => 3;

        public async Task<DocumentationRequest> ExecuteAsync(DocumentationRequest input, PipelineContext context)
        {
            try
            {
                _logger.LogInformation("Starting AI documentation generation for {Language} code", input.Language);

                // Validate input
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogWarning("Empty code provided for documentation generation");
                    input.Result = new Nexo.Core.Domain.Results.DocumentationResult
                    {
                        IsSuccess = true
                    };
                    return input;
                }

                // Create AI operation context
                var aiContext = new AIOperationContext
                {
                    OperationType = AIOperationType.Documentation,
                    TargetPlatform = ConvertToEnumsPlatformType(context.EnvironmentProfile?.CurrentPlatform ?? Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unknown),
                    MaxTokens = 3072,
                    Temperature = 0.4, // Moderate temperature for balanced creativity and consistency
                    Priority = AIPriority.Quality.ToString(),
                    Requirements = new Nexo.Core.Domain.Entities.AI.AIRequirements
                    {
                        Priority = AIPriority.Quality,
                        SafetyLevel = Nexo.Core.Domain.Enums.Safety.SafetyLevel.Medium,
                        RequiresHighQuality = true,
                        MaxTokens = 3072,
                        Temperature = 0.4
                    }
                };

                // Select optimal AI engine
                var selection = await _runtimeSelector.SelectOptimalProviderAsync(aiContext);
                if (selection == null)
                {
                    _logger.LogError("No suitable AI provider found for documentation generation");
                    throw new InvalidOperationException("No AI provider available for documentation generation");
                }

                // Create AI engine
                var engine = await selection.CreateEngineAsync(aiContext);
                if (engine is not IAIEngine aiEngine)
                {
                    _logger.LogError("Failed to create AI engine for documentation generation");
                    throw new InvalidOperationException("Failed to create AI engine for documentation generation");
                }

                // Initialize engine if needed
                if (!aiEngine.IsInitialized)
                {
                    var model = new ModelInfo { Id = "mock-model", Name = "Mock Model" };
                    await aiEngine.InitializeAsync(model, aiContext);
                }

                // Generate documentation
                var documentation = await aiEngine.GenerateDocumentationAsync(input.Code, aiContext);

                // Enhance documentation with additional analysis
                var enhancedDocumentation = await EnhanceDocumentationAsync(documentation, input, context);

                // Apply safety validation
                var validatedDocumentation = await ApplySafetyValidationAsync(enhancedDocumentation, input, context);

                // Create documentation result
                var result = new Nexo.Core.Domain.Results.DocumentationResult
                {
                    IsSuccess = true
                };

                // Update input with results
                input.Result = result;
                input.DocumentationCompleted = true;
                input.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("AI documentation generation completed successfully");

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI documentation generation");
                
                // Create fallback result
                input.Result = new Nexo.Core.Domain.Results.DocumentationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
                input.DocumentationCompleted = false;
                
                return input;
            }
        }

        private string GetModelPathForDocumentation(AIEngineType engineType)
        {
            return engineType switch
            {
                AIEngineType.LlamaWebAssembly => "models/codellama-7b-instruct.gguf",
                AIEngineType.LlamaNative => "models/codellama-13b-instruct.gguf",
                _ => "models/codellama-7b-instruct.gguf"
            };
        }

        private async Task<string> EnhanceDocumentationAsync(string documentation, DocumentationRequest request, PipelineContext context)
        {
            _logger.LogDebug("Enhancing documentation with additional analysis");

            var enhancedDocumentation = documentation;

            // Add code analysis insights
            var analysisInsights = await GenerateCodeAnalysisInsightsAsync(request.Code, 
                Enum.TryParse<Nexo.Core.Domain.Enums.Code.CodeLanguage>(request.Language, out var lang) ? lang : Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp);
            if (analysisInsights.Any())
            {
                enhancedDocumentation += "\n\n## Code Analysis Insights\n" + string.Join("\n", analysisInsights);
            }

            // Add performance considerations
            var performanceNotes = await GeneratePerformanceNotesAsync(request.Code, 
                Enum.TryParse<Nexo.Core.Domain.Enums.Code.CodeLanguage>(request.Language, out var lang2) ? lang2 : Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp);
            if (performanceNotes.Any())
            {
                enhancedDocumentation += "\n\n## Performance Considerations\n" + string.Join("\n", performanceNotes);
            }

            // Add usage examples
            var usageExamples = await GenerateUsageExamplesAsync(request.Code, 
                Enum.TryParse<Nexo.Core.Domain.Enums.Code.CodeLanguage>(request.Language, out var lang3) ? lang3 : Nexo.Core.Domain.Enums.Code.CodeLanguage.CSharp);
            if (usageExamples.Any())
            {
                enhancedDocumentation += "\n\n## Usage Examples\n" + string.Join("\n", usageExamples);
            }

            // Add context-specific documentation
            var contextDocumentation = await GenerateContextDocumentationAsync(request, context);
            if (!string.IsNullOrEmpty(contextDocumentation))
            {
                enhancedDocumentation += "\n\n## Context Information\n" + contextDocumentation;
            }

            return enhancedDocumentation;
        }

        private async Task<string> ApplySafetyValidationAsync(string documentation, DocumentationRequest request, PipelineContext context)
        {
            _logger.LogDebug("Applying safety validation to documentation");

            // Filter out potentially harmful content
            var filteredDocumentation = await FilterDocumentationContentAsync(documentation, request, context);

            // Validate documentation quality
            var validatedDocumentation = await ValidateDocumentationQualityAsync(filteredDocumentation, request, context);

            return validatedDocumentation;
        }

        private async Task<List<string>> GenerateCodeAnalysisInsightsAsync(string code, CodeLanguage language)
        {
            // In a real implementation, this would analyze code and generate insights
            await Task.Delay(100);

            var insights = new List<string>();

            // Analyze code structure
            if (code.Contains("class"))
            {
                insights.Add("- This code defines a class with object-oriented design");
            }

            if (code.Contains("async") && code.Contains("await"))
            {
                insights.Add("- This code uses asynchronous programming patterns");
            }

            if (code.Contains("LINQ"))
            {
                insights.Add("- This code utilizes LINQ for data manipulation");
            }

            if (code.Contains("try") && code.Contains("catch"))
            {
                insights.Add("- This code includes proper error handling");
            }

            return insights;
        }

        private async Task<List<string>> GeneratePerformanceNotesAsync(string code, CodeLanguage language)
        {
            // In a real implementation, this would analyze performance characteristics
            await Task.Delay(100);

            var notes = new List<string>();

            // Check for performance characteristics
            if (code.Contains("for (int i = 0; i < items.Count; i++)"))
            {
                notes.Add("- Consider using foreach for better readability and performance");
            }

            if (code.Contains("string +"))
            {
                notes.Add("- String concatenation in loops may impact performance; consider StringBuilder");
            }

            if (code.Contains("LINQ") && code.Contains("ToList()"))
            {
                notes.Add("- LINQ operations create intermediate collections; consider streaming alternatives");
            }

            if (code.Contains("new List") && code.Contains("Add"))
            {
                notes.Add("- Pre-allocate List capacity if size is known to avoid reallocations");
            }

            return notes;
        }

        private async Task<List<string>> GenerateUsageExamplesAsync(string code, CodeLanguage language)
        {
            // In a real implementation, this would generate usage examples
            await Task.Delay(100);

            var examples = new List<string>();

            // Generate basic usage example
            if (code.Contains("public class"))
            {
                examples.Add("```csharp\n// Basic usage example\nvar instance = new MyClass();\nvar result = instance.MyMethod();\n```");
            }

            if (code.Contains("public static"))
            {
                examples.Add("```csharp\n// Static method usage\nvar result = MyClass.StaticMethod();\n```");
            }

            if (code.Contains("async"))
            {
                examples.Add("```csharp\n// Asynchronous usage\nvar result = await instance.AsyncMethodAsync();\n```");
            }

            return examples;
        }

        private async Task<string> GenerateContextDocumentationAsync(DocumentationRequest request, PipelineContext context)
        {
            // In a real implementation, this would generate context-specific documentation
            await Task.Delay(50);

            var contextInfo = new List<string>();

            // Add platform-specific information
            if (context.EnvironmentProfile?.CurrentPlatform == Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly)
            {
                contextInfo.Add("- This code is optimized for WebAssembly execution");
                contextInfo.Add("- Consider browser compatibility when using this code");
            }

            if (context.EnvironmentProfile?.CurrentPlatform == Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows)
            {
                contextInfo.Add("- This code is optimized for Windows platform");
                contextInfo.Add("- Consider Windows-specific APIs for enhanced functionality");
            }

            // Add documentation type specific information
            if (request.DocumentationType == "API")
            {
                contextInfo.Add("- This is API documentation for external consumers");
                contextInfo.Add("- Include parameter descriptions and return value information");
            }

            if (request.DocumentationType == "Internal")
            {
                contextInfo.Add("- This is internal documentation for development team");
                contextInfo.Add("- Include implementation details and design decisions");
            }

            return string.Join("\n", contextInfo);
        }

        private async Task<string> FilterDocumentationContentAsync(string documentation, DocumentationRequest request, PipelineContext context)
        {
            // In a real implementation, this would filter potentially harmful content
            await Task.Delay(50);

            // Remove or replace potentially harmful content
            var filteredDocumentation = documentation
                .Replace("dangerous", "risky")
                .Replace("unsafe", "requires caution")
                .Replace("hack", "workaround");

            return filteredDocumentation;
        }

        private async Task<string> ValidateDocumentationQualityAsync(string documentation, DocumentationRequest request, PipelineContext context)
        {
            // In a real implementation, this would validate documentation quality
            await Task.Delay(50);

            // Ensure documentation meets quality standards
            if (documentation.Length < 100)
            {
                documentation += "\n\n*Note: This documentation could be expanded with more detailed information.*";
            }

            if (!documentation.Contains("##") && !documentation.Contains("###"))
            {
                documentation = "## Overview\n\n" + documentation;
            }

            return documentation;
        }

        private int CalculateDocumentationQuality(string documentation, DocumentationRequest request)
        {
            var score = 0;

            // Base score
            score += 20;

            // Length bonus
            if (documentation.Length > 500) score += 20;
            if (documentation.Length > 1000) score += 10;

            // Structure bonus
            if (documentation.Contains("##")) score += 15;
            if (documentation.Contains("###")) score += 10;
            if (documentation.Contains("```")) score += 15;

            // Content quality bonus
            if (documentation.Contains("example")) score += 10;
            if (documentation.Contains("note")) score += 5;
            if (documentation.Contains("warning")) score += 5;

            return Math.Min(100, score);
        }

        private int CalculateDocumentationCoverage(string documentation, string code)
        {
            // In a real implementation, this would calculate actual coverage
            var codeLines = code.Split('\n').Length;
            var docLines = documentation.Split('\n').Length;
            
            // Simple coverage calculation
            var coverage = Math.Min(100, (docLines * 100) / Math.Max(1, codeLines));
            
            return coverage;
        }

        public async Task<bool> CanExecuteAsync(DocumentationRequest input, PipelineContext context)
        {
            try
            {
                // Check if input is valid
                if (string.IsNullOrWhiteSpace(input.Code))
                {
                    _logger.LogDebug("Cannot execute documentation step: empty code provided");
                    return false;
                }

                // Check if AI runtime is available
                var providers = await _runtimeSelector.GetAvailableProvidersAsync();
                if (!providers.Any())
                {
                    _logger.LogDebug("Cannot execute documentation step: no AI providers available");
                    return false;
                }

                // Check if context is valid
                if (context == null)
                {
                    _logger.LogDebug("Cannot execute documentation step: null context provided");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if documentation step can execute");
                return false;
            }
        }

        private Nexo.Core.Domain.Enums.PlatformType ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType platformType)
        {
            return platformType switch
            {
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web => Nexo.Core.Domain.Enums.PlatformType.Web,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop => Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile => Nexo.Core.Domain.Enums.PlatformType.Mobile,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Console => Nexo.Core.Domain.Enums.PlatformType.Desktop, // Map Console to Desktop
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows => Nexo.Core.Domain.Enums.PlatformType.Windows,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux => Nexo.Core.Domain.Enums.PlatformType.Linux,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS => Nexo.Core.Domain.Enums.PlatformType.macOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly => Nexo.Core.Domain.Enums.PlatformType.Web, // Map WebAssembly to Web
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS => Nexo.Core.Domain.Enums.PlatformType.iOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android => Nexo.Core.Domain.Enums.PlatformType.Android,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Cloud => Nexo.Core.Domain.Enums.PlatformType.Cloud,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Docker => Nexo.Core.Domain.Enums.PlatformType.Container,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Other => Nexo.Core.Domain.Enums.PlatformType.CrossPlatform,
                _ => Nexo.Core.Domain.Enums.PlatformType.Unknown
            };
        }
    }


    /// <summary>
    /// Documentation result from AI pipeline processing
    /// </summary>
    public class DocumentationResult
    {
        public string GeneratedDocumentation { get; set; } = string.Empty;
        public DocumentationType DocumentationType { get; set; }
        public int QualityScore { get; set; }
        public int Coverage { get; set; }
        public DateTime GenerationTime { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public AIEngineType EngineType { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of documentation
    /// </summary>
    public enum DocumentationType
    {
        API,
        Internal,
        User,
        Technical,
        Tutorial
    }
}
