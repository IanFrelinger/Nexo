using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Models.RAG;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// RAG-enhanced code generation agent specialized for C# and .NET development
    /// </summary>
    public class RAGEnhancedCodeGenerationAgent : BaseAiEnhancedAgent
    {
        private readonly IDocumentationRAGService _ragService;
        private readonly ILogger<RAGEnhancedCodeGenerationAgent> _logger;

        public RAGEnhancedCodeGenerationAgent(
            AgentId id,
            AgentName name,
            AgentRole role,
            IModelOrchestrator modelOrchestrator,
            IDocumentationRAGService ragService,
            ILogger<RAGEnhancedCodeGenerationAgent> logger)
            : base(id, name, role, modelOrchestrator, logger)
        {
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure AI capabilities for code generation
            AiCapabilities.CanGenerateCode = true;
            AiCapabilities.CanAnalyzeTasks = true;
            AiCapabilities.CanProvideSuggestions = true;
            AiCapabilities.CanReviewCode = true;
            AiCapabilities.CanGenerateDocumentation = true;
            
            // Set focus areas for code generation
            FocusAreas.AddRange(new[]
            {
                "C# Code Generation",
                "API Development",
                "Data Access",
                "Business Logic",
                "Testing Code",
                "Configuration",
                "Dependency Injection",
                "Error Handling",
                "Performance Optimization",
                "Best Practices"
            });
        }

        public override async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Processing RAG-enhanced code generation request: {RequestType}", request.Type);

            try
            {
                Status = AgentStatus.Busy;

                // Always use RAG for code generation to ensure best practices
                var response = await ProcessCodeGenerationWithRAGAsync(request, ct);
                
                Status = AgentStatus.Active;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG-enhanced code generation request");
                Status = AgentStatus.Failed;
                throw;
            }
        }

        private async Task<AgentResponse> ProcessCodeGenerationWithRAGAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Extract code generation requirements from the request
                var requirements = ExtractCodeGenerationRequirements(request);
                
                // Query RAG system for relevant code patterns and best practices
                var ragQuery = new RAGQuery
                {
                    Query = BuildCodeGenerationQuery(requirements),
                    ContextType = DocumentationContextType.CodeGeneration,
                    MaxResults = 8,
                    SimilarityThreshold = 0.7,
                    Filters = CreateCodeGenerationFilters(requirements)
                };

                var ragResponse = await _ragService.QueryDocumentationAsync(ragQuery);

                // Build enhanced prompt with code patterns and best practices
                var enhancedPrompt = BuildCodeGenerationPrompt(request, requirements, ragResponse);

                // Process with AI using enhanced context
                var enhancedRequest = request with { Input = enhancedPrompt };
                var response = await base.ProcessRequestAsync(enhancedRequest, cancellationToken);

                // Add RAG metadata
                response.Metadata = response.Metadata ?? new Dictionary<string, object>();
                response.Metadata["RAGUsed"] = true;
                response.Metadata["RAGConfidence"] = ragResponse.ConfidenceScore;
                response.Metadata["CodePatternsRetrieved"] = ragResponse.RetrievedChunks.Count;
                response.Metadata["BestPracticesApplied"] = true;
                response.Metadata["RAGContext"] = ragResponse.Context;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RAG code generation failed, falling back to standard processing");
                return await base.ProcessRequestAsync(request, cancellationToken);
            }
        }

        private CodeGenerationRequirements ExtractCodeGenerationRequirements(AgentRequest request)
        {
            var input = request.Input.ToLowerInvariant();
            
            return new CodeGenerationRequirements
            {
                Language = ExtractLanguage(input),
                Framework = ExtractFramework(input),
                Pattern = ExtractPattern(input),
                Features = ExtractFeatures(input),
                Version = ExtractVersion(input),
                Context = request.Context
            };
        }

        private string ExtractLanguage(string input)
        {
            if (input.Contains("c#") || input.Contains("csharp"))
                return "C#";
            if (input.Contains("f#") || input.Contains("fsharp"))
                return "F#";
            if (input.Contains("vb") || input.Contains("visual basic"))
                return "VB.NET";
            
            return "C#"; // Default
        }

        private string ExtractFramework(string input)
        {
            if (input.Contains("asp.net") || input.Contains("aspnet"))
                return "ASP.NET";
            if (input.Contains("wpf"))
                return "WPF";
            if (input.Contains("winforms"))
                return "WinForms";
            if (input.Contains("maui"))
                return "MAUI";
            if (input.Contains("blazor"))
                return "Blazor";
            if (input.Contains("xamarin"))
                return "Xamarin";
            
            return ".NET"; // Default
        }

        private string ExtractPattern(string input)
        {
            if (input.Contains("controller") || input.Contains("api"))
                return "API Controller";
            if (input.Contains("service") || input.Contains("business logic"))
                return "Service";
            if (input.Contains("repository"))
                return "Repository";
            if (input.Contains("model") || input.Contains("entity"))
                return "Model/Entity";
            if (input.Contains("test") || input.Contains("unit test"))
                return "Test";
            if (input.Contains("middleware"))
                return "Middleware";
            if (input.Contains("configuration"))
                return "Configuration";
            
            return "General";
        }

        private List<string> ExtractFeatures(string input)
        {
            var features = new List<string>();
            
            if (input.Contains("async") || input.Contains("await"))
                features.Add("Async/Await");
            if (input.Contains("linq"))
                features.Add("LINQ");
            if (input.Contains("dependency injection") || input.Contains("di"))
                features.Add("Dependency Injection");
            if (input.Contains("logging"))
                features.Add("Logging");
            if (input.Contains("validation"))
                features.Add("Validation");
            if (input.Contains("error handling") || input.Contains("exception"))
                features.Add("Error Handling");
            if (input.Contains("caching"))
                features.Add("Caching");
            if (input.Contains("authentication") || input.Contains("authorization"))
                features.Add("Security");
            if (input.Contains("performance") || input.Contains("optimization"))
                features.Add("Performance");
            
            return features;
        }

        private string ExtractVersion(string input)
        {
            if (input.Contains("net8") || input.Contains(".net 8"))
                return "8.0";
            if (input.Contains("net6") || input.Contains(".net 6"))
                return "6.0";
            if (input.Contains("net5") || input.Contains(".net 5"))
                return "5.0";
            if (input.Contains("net core 3.1"))
                return "3.1";
            if (input.Contains("net framework 4.8"))
                return "4.8";
            
            return "8.0"; // Default to latest
        }

        private string BuildCodeGenerationQuery(CodeGenerationRequirements requirements)
        {
            var queryParts = new List<string>
            {
                $"C# {requirements.Language}",
                $"{requirements.Framework}",
                $"{requirements.Pattern}",
                "code generation",
                "best practices"
            };

            queryParts.AddRange(requirements.Features);

            return string.Join(" ", queryParts);
        }

        private List<DocumentationFilter> CreateCodeGenerationFilters(CodeGenerationRequirements requirements)
        {
            var filters = new List<DocumentationFilter>();

            if (!string.IsNullOrEmpty(requirements.Version))
            {
                filters.Add(new DocumentationFilter
                {
                    Field = "Version",
                    Value = requirements.Version,
                    Operator = FilterOperator.Equals
                });
            }

            if (requirements.Framework != ".NET")
            {
                filters.Add(new DocumentationFilter
                {
                    Field = "DocumentationType",
                    Value = "Framework",
                    Operator = FilterOperator.Equals
                });
            }

            return filters;
        }

        private string BuildCodeGenerationPrompt(AgentRequest request, CodeGenerationRequirements requirements, RAGResponse ragResponse)
        {
            return $@"
## Code Generation Request
{request.Input}

## Requirements Analysis
- **Language**: {requirements.Language}
- **Framework**: {requirements.Framework}
- **Pattern**: {requirements.Pattern}
- **Version**: {requirements.Version}
- **Features**: {string.Join(", ", requirements.Features)}

## Relevant Documentation and Best Practices
{ragResponse.Context}

## Code Generation Instructions
1. Generate high-quality, production-ready code based on the requirements above
2. Follow the best practices and patterns shown in the documentation context
3. Include proper error handling, logging, and validation where appropriate
4. Use modern C# features and .NET patterns
5. Include comprehensive comments and documentation
6. Ensure the code follows SOLID principles and clean architecture
7. Include unit tests if applicable
8. Provide usage examples

## Additional Context
- Request Type: {request.Type}
- Priority: {request.Priority}
- Target Framework: {requirements.Framework} {requirements.Version}

Please generate the requested code with full implementation details, following the patterns and best practices from the documentation context.
";
        }

        private class CodeGenerationRequirements
        {
            public string Language { get; set; } = "C#";
            public string Framework { get; set; } = ".NET";
            public string Pattern { get; set; } = "General";
            public List<string> Features { get; set; } = new List<string>();
            public string Version { get; set; } = "8.0";
            public Dictionary<string, object>? Context { get; set; }
        }
    }
}
