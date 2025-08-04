using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Models;

using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// AI-enhanced developer agent with specialized capabilities for development tasks.
    /// </summary>
    public class AIEnhancedDeveloperAgent : BaseAIEnhancedAgent
    {
        public AIEnhancedDeveloperAgent(
            IModelOrchestrator modelOrchestrator,
            ILogger<AIEnhancedDeveloperAgent> logger)
            : base(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("AI-Enhanced Developer Agent"),
                new AgentRole("Developer"),
                modelOrchestrator,
                logger)
        {
            // Initialize developer-specific capabilities
            Capabilities.AddRange(new[]
            {
                "Code Development",
                "Code Review",
                "Bug Fixing",
                "Unit Testing",
                "Integration Testing",
                "Performance Optimization",
                "Code Refactoring",
                "Documentation",
                "API Development",
                "Database Design"
            });

            FocusAreas.AddRange(new[]
            {
                "C# Development",
                ".NET Framework",
                "ASP.NET Core",
                "Entity Framework",
                "Web APIs",
                "Microservices",
                "Cloud Development",
                "DevOps",
                "Testing",
                "Code Quality"
            });

            // Configure AI capabilities
            AICapabilities.CanAnalyzeCode = true;
            AICapabilities.CanGenerateCode = true;
            AICapabilities.CanAnalyzeTasks = true;
            AICapabilities.CanProvideSuggestions = true;
            AICapabilities.CanSolveProblems = true;
            AICapabilities.PreferredModel = "gpt-4";
            AICapabilities.ProcessingStrategy = AIProcessingStrategy.Advanced;
        }

        protected override async Task<AgentResponse> ProcessRequestInternalAsync(AgentRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Processing developer request: {RequestType}", request.Type);

            switch (request.Type)
            {
                case AgentRequestType.CodeReview:
                    return await HandleCodeReviewAsync(request, ct);
                case AgentRequestType.BugFix:
                    return await HandleBugFixAsync(request, ct);
                case AgentRequestType.FeatureImplementation:
                    return await HandleCodeGenerationAsync(request, ct);
                case AgentRequestType.TestCreation:
                    return await HandleTestingAsync(request, ct);
                case AgentRequestType.Analysis:
                    return await HandleRefactoringAsync(request, ct);
                case AgentRequestType.Documentation:
                    return await HandleDocumentationAsync(request, ct);
                default:
                    return await HandleGenericRequestAsync(request, ct);
            }
        }

        protected override async Task OnStartedAsync(CancellationToken ct)
        {
            _logger.LogInformation("AI-Enhanced Developer Agent started");
            await Task.CompletedTask;
        }

        protected override async Task OnStoppedAsync(CancellationToken ct)
        {
            _logger.LogInformation("AI-Enhanced Developer Agent stopped");
            await Task.CompletedTask;
        }

        private async Task<AgentResponse> HandleCodeReviewAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Code review completed with AI assistance"
            };

            // Extract code content from request
            if (request.Context?.ContainsKey("code") == true && request.Context["code"] is string code && !string.IsNullOrWhiteSpace(code))
            {
                var reviewResult = await PerformAICodeReviewAsync(code, ct);
                response = new AgentResponse
                {
                    Success = true,
                    Content = reviewResult
                };
            }
            else
            {
                return new AgentResponse { Success = false, Content = "Missing or invalid code context." };
            }

            return response;
        }

        private async Task<AgentResponse> HandleBugFixAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Bug fix analysis completed"
            };

            if (request.Context?.ContainsKey("error_message") == true && 
                request.Context?.ContainsKey("code_context") == true)
            {
                var errorMessage = request.Context["error_message"].ToString();
                var codeContext = request.Context["code_context"].ToString();
                var fixSuggestion = await GenerateBugFixAsync(errorMessage, codeContext, ct);
                response = new AgentResponse
                {
                    Success = true,
                    Content = fixSuggestion
                };
            }

            return response;
        }

        private async Task<AgentResponse> HandleCodeGenerationAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Code generation completed"
            };

            if (request.Context?.ContainsKey("requirements") == true)
            {
                var requirements = request.Context["requirements"].ToString();
                var generatedCode = await GenerateCodeAsync(requirements, ct);
                response = new AgentResponse
                {
                    Success = true,
                    Content = generatedCode
                };
            }

            return response;
        }

        private async Task<AgentResponse> HandleTestingAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Test generation completed"
            };

            if (request.Context?.ContainsKey("code_to_test") == true)
            {
                var codeToTest = request.Context["code_to_test"].ToString();
                var testCode = await GenerateTestsAsync(codeToTest, ct);
                response = new AgentResponse
                {
                    Success = true,
                    Content = testCode
                };
            }

            return response;
        }

        private async Task<AgentResponse> HandleRefactoringAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Refactoring suggestions generated"
            };

            if (request.Context?.ContainsKey("code_to_refactor") == true)
            {
                var codeToRefactor = request.Context["code_to_refactor"].ToString();
                var refactoredCode = await SuggestRefactoringAsync(codeToRefactor, ct);
                response = new AgentResponse
                {
                    Success = true,
                    Content = refactoredCode
                };
            }

            return response;
        }

        private async Task<AgentResponse> HandleDocumentationAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Documentation generated"
            };

            if (request.Context?.ContainsKey("code_to_document") == true)
            {
                var codeToDocument = request.Context["code_to_document"].ToString();
                var documentation = await GenerateDocumentationAsync(codeToDocument, ct);
                response = new AgentResponse
                {
                    Success = true,
                    Content = documentation
                };
            }

            return response;
        }

        private async Task<AgentResponse> HandleGenericRequestAsync(AgentRequest request, CancellationToken ct)
        {
            return new AgentResponse
            {
                Success = true,
                Content = $"Developer agent processed request: {request.Content}"
            };
        }

        private async Task<string> PerformAICodeReviewAsync(string code, CancellationToken ct)
        {
            var prompt = $@"Perform a comprehensive code review for the following C# code:

{code}

Please provide:
1. Code quality assessment
2. Potential issues and bugs
3. Performance considerations
4. Security concerns
5. Best practices recommendations
6. Specific improvement suggestions

Format your response in a clear, structured manner.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        private async Task<string> GenerateBugFixAsync(string errorMessage, string codeContext, CancellationToken ct)
        {
            var prompt = $@"Analyze the following error and code context to provide a bug fix:

Error Message: {errorMessage}

Code Context:
{codeContext}

Please provide:
1. Root cause analysis
2. Specific fix implementation
3. Explanation of the fix
4. Prevention strategies

Format your response with clear code examples.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 1500,
                Temperature = 0.3
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        private async Task<string> GenerateCodeAsync(string requirements, CancellationToken ct)
        {
            var prompt = $@"Generate C# code based on the following requirements:

Requirements: {requirements}

Please provide:
1. Complete, compilable C# code
2. XML documentation comments
3. Unit tests
4. Usage examples

Ensure the code follows C# best practices and design patterns.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 3000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        private async Task<string> GenerateTestsAsync(string codeToTest, CancellationToken ct)
        {
            var prompt = $@"Generate comprehensive unit tests for the following C# code:

{codeToTest}

Please provide:
1. Unit tests using MSTest, NUnit, or xUnit
2. Test cases covering normal scenarios
3. Test cases covering edge cases
4. Test cases covering error conditions
5. Mock setup examples where applicable

Ensure good test coverage and follow testing best practices.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2500,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        private async Task<string> SuggestRefactoringAsync(string codeToRefactor, CancellationToken ct)
        {
            var prompt = $@"Suggest refactoring improvements for the following C# code:

{codeToRefactor}

Please provide:
1. Code quality issues identified
2. Specific refactoring suggestions
3. Refactored code examples
4. Benefits of each refactoring
5. Potential risks and considerations

Focus on improving readability, maintainability, and performance.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        private async Task<string> GenerateDocumentationAsync(string codeToDocument, CancellationToken ct)
        {
            var prompt = $@"Generate comprehensive documentation for the following C# code:

{codeToDocument}

Please provide:
1. XML documentation comments for all public members
2. Class-level documentation
3. Method-level documentation
4. Parameter and return value documentation
5. Usage examples
6. Architecture overview if applicable

Follow Microsoft documentation standards.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }
    }
} 