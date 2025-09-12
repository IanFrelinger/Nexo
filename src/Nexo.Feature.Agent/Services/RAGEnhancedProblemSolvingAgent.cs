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
    /// RAG-enhanced problem-solving agent specialized for debugging and troubleshooting C#/.NET issues
    /// </summary>
    public class RAGEnhancedProblemSolvingAgent : BaseAiEnhancedAgent
    {
        private readonly IDocumentationRAGService _ragService;
        private readonly ILogger<RAGEnhancedProblemSolvingAgent> _logger;

        public RAGEnhancedProblemSolvingAgent(
            AgentId id,
            AgentName name,
            AgentRole role,
            IModelOrchestrator modelOrchestrator,
            IDocumentationRAGService ragService,
            ILogger<RAGEnhancedProblemSolvingAgent> logger)
            : base(id, name, role, modelOrchestrator, logger)
        {
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure AI capabilities for problem solving
            AiCapabilities.CanSolveProblems = true;
            AiCapabilities.CanAnalyzeTasks = true;
            AiCapabilities.CanProvideSuggestions = true;
            AiCapabilities.CanReviewCode = true;
            
            // Set focus areas for problem solving
            FocusAreas.AddRange(new[]
            {
                "Debugging",
                "Error Resolution",
                "Performance Issues",
                "Memory Problems",
                "Threading Issues",
                "Async/Await Problems",
                "Dependency Injection Issues",
                "Configuration Problems",
                "Migration Issues",
                "Best Practices"
            });
        }

        public override async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Processing RAG-enhanced problem-solving request: {RequestType}", request.Type);

            try
            {
                Status = AgentStatus.Busy;

                // Always use RAG for problem solving to find relevant solutions
                var response = await ProcessProblemSolvingWithRAGAsync(request, ct);
                
                Status = AgentStatus.Active;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG-enhanced problem-solving request");
                Status = AgentStatus.Failed;
                throw;
            }
        }

        private async Task<AgentResponse> ProcessProblemSolvingWithRAGAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Analyze the problem to extract key information
                var problemAnalysis = AnalyzeProblem(request);
                
                // Query RAG system for relevant solutions and troubleshooting guides
                var ragQuery = new RAGQuery
                {
                    Query = BuildProblemSolvingQuery(problemAnalysis),
                    ContextType = DocumentationContextType.ProblemSolving,
                    MaxResults = 6,
                    SimilarityThreshold = 0.6,
                    Filters = CreateProblemSolvingFilters(problemAnalysis)
                };

                var ragResponse = await _ragService.QueryDocumentationAsync(ragQuery);

                // Build enhanced prompt with troubleshooting context
                var enhancedPrompt = BuildProblemSolvingPrompt(request, problemAnalysis, ragResponse);

                // Process with AI using enhanced context
                var enhancedRequest = request with { Input = enhancedPrompt };
                var response = await base.ProcessRequestAsync(enhancedRequest, cancellationToken);

                // Add RAG metadata
                response.Metadata = response.Metadata ?? new Dictionary<string, object>();
                response.Metadata["RAGUsed"] = true;
                response.Metadata["RAGConfidence"] = ragResponse.ConfidenceScore;
                response.Metadata["SolutionsRetrieved"] = ragResponse.RetrievedChunks.Count;
                response.Metadata["ProblemType"] = problemAnalysis.ProblemType;
                response.Metadata["RAGContext"] = ragResponse.Context;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RAG problem solving failed, falling back to standard processing");
                return await base.ProcessRequestAsync(request, cancellationToken);
            }
        }

        private ProblemAnalysis AnalyzeProblem(AgentRequest request)
        {
            var input = request.Input.ToLowerInvariant();
            
            return new ProblemAnalysis
            {
                ProblemType = DetermineProblemType(input),
                ErrorMessages = ExtractErrorMessages(input),
                Technologies = ExtractTechnologies(input),
                Symptoms = ExtractSymptoms(input),
                Context = request.Context
            };
        }

        private string DetermineProblemType(string input)
        {
            if (input.Contains("exception") || input.Contains("error") || input.Contains("throw"))
                return "Exception";
            if (input.Contains("performance") || input.Contains("slow") || input.Contains("timeout"))
                return "Performance";
            if (input.Contains("memory") || input.Contains("leak") || input.Contains("out of memory"))
                return "Memory";
            if (input.Contains("thread") || input.Contains("deadlock") || input.Contains("race condition"))
                return "Threading";
            if (input.Contains("async") || input.Contains("await") || input.Contains("deadlock"))
                return "Async/Await";
            if (input.Contains("dependency injection") || input.Contains("di") || input.Contains("service"))
                return "Dependency Injection";
            if (input.Contains("configuration") || input.Contains("config") || input.Contains("settings"))
                return "Configuration";
            if (input.Contains("migration") || input.Contains("upgrade") || input.Contains("compatibility"))
                return "Migration";
            if (input.Contains("authentication") || input.Contains("authorization") || input.Contains("security"))
                return "Security";
            if (input.Contains("database") || input.Contains("entity framework") || input.Contains("sql"))
                return "Database";
            
            return "General";
        }

        private List<string> ExtractErrorMessages(string input)
        {
            var errorMessages = new List<string>();
            
            // Look for common error patterns
            var errorPatterns = new[]
            {
                "nullreferenceexception",
                "argumentnullexception",
                "invalidoperationexception",
                "notimplementedexception",
                "timeoutexception",
                "httpexception",
                "sqlException",
                "unauthorizedaccessexception",
                "filenotfoundexception",
                "directorynotfoundexception"
            };

            foreach (var pattern in errorPatterns)
            {
                if (input.Contains(pattern))
                {
                    errorMessages.Add(pattern);
                }
            }

            return errorMessages;
        }

        private List<string> ExtractTechnologies(string input)
        {
            var technologies = new List<string>();
            
            if (input.Contains("asp.net") || input.Contains("aspnet"))
                technologies.Add("ASP.NET");
            if (input.Contains("entity framework") || input.Contains("ef"))
                technologies.Add("Entity Framework");
            if (input.Contains("sql server") || input.Contains("sqlserver"))
                technologies.Add("SQL Server");
            if (input.Contains("redis"))
                technologies.Add("Redis");
            if (input.Contains("rabbitmq"))
                technologies.Add("RabbitMQ");
            if (input.Contains("docker"))
                technologies.Add("Docker");
            if (input.Contains("kubernetes") || input.Contains("k8s"))
                technologies.Add("Kubernetes");
            if (input.Contains("azure"))
                technologies.Add("Azure");
            if (input.Contains("aws"))
                technologies.Add("AWS");
            if (input.Contains("blazor"))
                technologies.Add("Blazor");
            if (input.Contains("signalr"))
                technologies.Add("SignalR");
            
            return technologies;
        }

        private List<string> ExtractSymptoms(string input)
        {
            var symptoms = new List<string>();
            
            if (input.Contains("crash") || input.Contains("crashes"))
                symptoms.Add("Application crashes");
            if (input.Contains("hang") || input.Contains("hangs") || input.Contains("freeze"))
                symptoms.Add("Application hangs");
            if (input.Contains("slow") || input.Contains("slower"))
                symptoms.Add("Performance degradation");
            if (input.Contains("timeout"))
                symptoms.Add("Timeout errors");
            if (input.Contains("memory") || input.Contains("leak"))
                symptoms.Add("Memory issues");
            if (input.Contains("cpu") || input.Contains("high cpu"))
                symptoms.Add("High CPU usage");
            if (input.Contains("disk") || input.Contains("io"))
                symptoms.Add("Disk I/O issues");
            if (input.Contains("network") || input.Contains("connection"))
                symptoms.Add("Network issues");
            
            return symptoms;
        }

        private string BuildProblemSolvingQuery(ProblemAnalysis analysis)
        {
            var queryParts = new List<string>
            {
                analysis.ProblemType,
                "troubleshooting",
                "debugging",
                "solution",
                "fix"
            };

            queryParts.AddRange(analysis.ErrorMessages);
            queryParts.AddRange(analysis.Technologies);
            queryParts.AddRange(analysis.Symptoms);

            return string.Join(" ", queryParts.Distinct());
        }

        private List<DocumentationFilter> CreateProblemSolvingFilters(ProblemAnalysis analysis)
        {
            var filters = new List<DocumentationFilter>();

            // Filter by problem type
            if (analysis.ProblemType != "General")
            {
                filters.Add(new DocumentationFilter
                {
                    Field = "DocumentationType",
                    Value = "Troubleshooting",
                    Operator = FilterOperator.Equals
                });
            }

            // Filter by technologies if specified
            if (analysis.Technologies.Any())
            {
                filters.Add(new DocumentationFilter
                {
                    Field = "Tags",
                    Value = string.Join(",", analysis.Technologies),
                    Operator = FilterOperator.Contains
                });
            }

            return filters;
        }

        private string BuildProblemSolvingPrompt(AgentRequest request, ProblemAnalysis analysis, RAGResponse ragResponse)
        {
            return $@"
## Problem Description
{request.Input}

## Problem Analysis
- **Problem Type**: {analysis.ProblemType}
- **Error Messages**: {string.Join(", ", analysis.ErrorMessages)}
- **Technologies**: {string.Join(", ", analysis.Technologies)}
- **Symptoms**: {string.Join(", ", analysis.Symptoms)}

## Relevant Troubleshooting Documentation
{ragResponse.Context}

## Troubleshooting Instructions
1. Analyze the problem based on the symptoms and error messages
2. Use the troubleshooting documentation above to identify potential solutions
3. Provide step-by-step debugging instructions
4. Suggest code fixes or configuration changes
5. Include preventive measures to avoid similar issues
6. Provide alternative solutions if the primary approach doesn't work
7. Include relevant code examples and best practices
8. Mention any version-specific considerations

## Additional Context
- Request Type: {request.Type}
- Priority: {request.Priority}
- Problem Category: {analysis.ProblemType}

Please provide a comprehensive troubleshooting guide based on the documentation context and problem analysis.
";
        }

        private class ProblemAnalysis
        {
            public string ProblemType { get; set; } = "General";
            public List<string> ErrorMessages { get; set; } = new List<string>();
            public List<string> Technologies { get; set; } = new List<string>();
            public List<string> Symptoms { get; set; } = new List<string>();
            public Dictionary<string, object>? Context { get; set; }
        }
    }
}
