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
    /// RAG-enhanced developer agent that leverages documentation knowledge base for enhanced responses
    /// </summary>
    public class RAGEnhancedDeveloperAgent : BaseAiEnhancedAgent
    {
        private readonly IDocumentationRAGService _ragService;
        private readonly ILogger<RAGEnhancedDeveloperAgent> _logger;

        public RAGEnhancedDeveloperAgent(
            AgentId id,
            AgentName name,
            AgentRole role,
            IModelOrchestrator modelOrchestrator,
            IDocumentationRAGService ragService,
            ILogger<RAGEnhancedDeveloperAgent> logger)
            : base(id, name, role, modelOrchestrator, logger)
        {
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure AI capabilities for RAG-enhanced responses
            AiCapabilities.CanAnalyzeTasks = true;
            AiCapabilities.CanGenerateCode = true;
            AiCapabilities.CanProvideSuggestions = true;
            AiCapabilities.CanSolveProblems = true;
            AiCapabilities.CanReviewCode = true;
            AiCapabilities.CanGenerateDocumentation = true;
            
            // Set focus areas
            FocusAreas.AddRange(new[]
            {
                "C# Development",
                ".NET Framework",
                ".NET Core",
                ".NET 5+",
                "Code Generation",
                "Problem Solving",
                "API Development",
                "Performance Optimization",
                "Best Practices",
                "Migration Guidance"
            });
        }

        public override async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Processing RAG-enhanced request for agent {AgentName}: {RequestType}", Name.Value, request.Type);

            try
            {
                Status = AgentStatus.Busy;

                // Determine if this request would benefit from RAG enhancement
                if (ShouldUseRAG(request))
                {
                    var response = await ProcessWithRAGAsync(request, ct);
                    Status = AgentStatus.Active;
                    return response;
                }
                else
                {
                    var response = await base.ProcessRequestAsync(request, ct);
                    Status = AgentStatus.Active;
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG-enhanced request for agent {AgentName}", Name.Value);
                Status = AgentStatus.Failed;
                throw;
            }
        }

        public override async Task<AiEnhancedAgentResponse> ProcessAiRequestAsync(AiEnhancedAgentRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing RAG-enhanced AI request for agent {AgentName}", Name.Value);

            try
            {
                Status = AgentStatus.Busy;

                // Convert to regular agent request for RAG processing
                var agentRequest = new AgentRequest
                {
                    Id = request.Id,
                    Type = request.Type,
                    Input = request.Input,
                    Context = request.Context,
                    Priority = request.Priority,
                    UseAi = true
                };

                var response = await ProcessWithRAGAsync(agentRequest, cancellationToken);

                // Convert back to AI-enhanced response
                var aiResponse = new AiEnhancedAgentResponse
                {
                    Id = response.Id,
                    Content = response.Content,
                    Success = response.Success,
                    AiWasUsed = true,
                    AiModelUsed = "RAG-Enhanced Model",
                    AiInsights = ExtractInsightsFromRAG(response),
                    AiConfidenceScore = CalculateRAGConfidence(response),
                    AiProcessingTimeMs = response.ProcessingTimeMs
                };

                Status = AgentStatus.Active;
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG-enhanced AI request for agent {AgentName}", Name.Value);
                Status = AgentStatus.Failed;
                throw;
            }
        }

        private async Task<AgentResponse> ProcessWithRAGAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Determine the appropriate context type based on request
                var contextType = DetermineContextType(request);

                // Query the RAG system for relevant documentation
                var ragQuery = new RAGQuery
                {
                    Query = request.Input,
                    ContextType = contextType,
                    MaxResults = 5,
                    SimilarityThreshold = 0.6,
                    Filters = CreateFiltersFromRequest(request)
                };

                var ragResponse = await _ragService.QueryDocumentationAsync(ragQuery);

                // Enhance the request with retrieved context
                var enhancedInput = BuildEnhancedInput(request, ragResponse);

                // Process with the base AI system using enhanced context
                var enhancedRequest = request with { Input = enhancedInput };
                var response = await base.ProcessRequestAsync(enhancedRequest, cancellationToken);

                // Add RAG metadata to the response
                response.Metadata = response.Metadata ?? new Dictionary<string, object>();
                response.Metadata["RAGUsed"] = true;
                response.Metadata["RAGConfidence"] = ragResponse.ConfidenceScore;
                response.Metadata["RAGChunksRetrieved"] = ragResponse.RetrievedChunks.Count;
                response.Metadata["RAGContext"] = ragResponse.Context;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RAG processing failed, falling back to standard processing");
                
                // Fall back to standard processing if RAG fails
                return await base.ProcessRequestAsync(request, cancellationToken);
            }
        }

        private bool ShouldUseRAG(AgentRequest request)
        {
            // Use RAG for requests that involve C#/.NET development
            var input = request.Input.ToLowerInvariant();
            
            var csharpKeywords = new[]
            {
                "c#", "csharp", ".net", "dotnet", "async", "await", "linq", "entity framework",
                "asp.net", "mvc", "web api", "minimal api", "dependency injection", "middleware",
                "controller", "model", "view", "service", "repository", "unit test", "integration test",
                "performance", "optimization", "memory", "threading", "parallel", "concurrent",
                "migration", "upgrade", "version", "framework", "runtime", "core", "standard"
            };

            return csharpKeywords.Any(keyword => input.Contains(keyword)) ||
                   request.Type.Contains("code") ||
                   request.Type.Contains("development") ||
                   request.Type.Contains("problem");
        }

        private DocumentationContextType DetermineContextType(AgentRequest request)
        {
            var input = request.Input.ToLowerInvariant();
            var type = request.Type.ToLowerInvariant();

            if (input.Contains("generate") || input.Contains("create") || input.Contains("write code"))
                return DocumentationContextType.CodeGeneration;
            
            if (input.Contains("problem") || input.Contains("error") || input.Contains("debug") || input.Contains("fix"))
                return DocumentationContextType.ProblemSolving;
            
            if (input.Contains("api") || input.Contains("method") || input.Contains("class") || input.Contains("interface"))
                return DocumentationContextType.APIReference;
            
            if (input.Contains("performance") || input.Contains("optimize") || input.Contains("memory"))
                return DocumentationContextType.PerformanceOptimization;
            
            if (input.Contains("migration") || input.Contains("upgrade") || input.Contains("migrate"))
                return DocumentationContextType.FrameworkSpecific;
            
            if (input.Contains("test") || input.Contains("testing") || input.Contains("unit") || input.Contains("integration"))
                return DocumentationContextType.Testing;
            
            if (input.Contains("security") || input.Contains("secure") || input.Contains("vulnerability"))
                return DocumentationContextType.Security;
            
            return DocumentationContextType.General;
        }

        private List<DocumentationFilter> CreateFiltersFromRequest(AgentRequest request)
        {
            var filters = new List<DocumentationFilter>();

            // Extract version information from context or input
            if (request.Context?.ContainsKey("TargetFramework") == true)
            {
                var framework = request.Context["TargetFramework"].ToString();
                if (framework?.Contains("net8") == true)
                {
                    filters.Add(new DocumentationFilter { Field = "Version", Value = "8.0", Operator = FilterOperator.Equals });
                }
                else if (framework?.Contains("net6") == true)
                {
                    filters.Add(new DocumentationFilter { Field = "Version", Value = "6.0", Operator = FilterOperator.Equals });
                }
            }

            // Extract runtime information
            if (request.Context?.ContainsKey("Runtime") == true)
            {
                var runtime = request.Context["Runtime"].ToString();
                if (!string.IsNullOrEmpty(runtime))
                {
                    filters.Add(new DocumentationFilter { Field = "Runtime", Value = runtime, Operator = FilterOperator.Equals });
                }
            }

            return filters;
        }

        private string BuildEnhancedInput(AgentRequest request, RAGResponse ragResponse)
        {
            var enhancedInput = $@"
## User Question
{request.Input}

## Relevant Documentation Context
{ragResponse.Context}

## Instructions
Please provide a comprehensive answer based on the documentation context above. If the context doesn't contain enough information, please indicate this clearly. Focus on practical, actionable advice with code examples when appropriate.

## Additional Context
- Request Type: {request.Type}
- Priority: {request.Priority}
- Agent: {Name.Value}
";

            return enhancedInput;
        }

        private List<string> ExtractInsightsFromRAG(AgentResponse response)
        {
            var insights = new List<string>();

            if (response.Metadata?.ContainsKey("RAGUsed") == true && 
                response.Metadata["RAGUsed"] is bool ragUsed && ragUsed)
            {
                insights.Add("Response enhanced with relevant documentation");
                
                if (response.Metadata.ContainsKey("RAGChunksRetrieved") && 
                    response.Metadata["RAGChunksRetrieved"] is int chunkCount)
                {
                    insights.Add($"Retrieved {chunkCount} relevant documentation chunks");
                }
                
                if (response.Metadata.ContainsKey("RAGConfidence") && 
                    response.Metadata["RAGConfidence"] is double confidence)
                {
                    insights.Add($"Documentation relevance confidence: {confidence:P1}");
                }
            }

            return insights;
        }

        private double CalculateRAGConfidence(AgentResponse response)
        {
            if (response.Metadata?.ContainsKey("RAGConfidence") == true && 
                response.Metadata["RAGConfidence"] is double confidence)
            {
                return confidence;
            }

            return 0.0;
        }
    }
}
