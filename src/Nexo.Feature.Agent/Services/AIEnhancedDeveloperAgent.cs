using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Models;

using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Represents an AI-enhanced developer agent designed to perform and manage advanced development tasks,
    /// leveraging AI models and orchestrated services for efficient operation.
    /// </summary>
    public class AiEnhancedDeveloperAgent : BaseAiEnhancedAgent
    {
        /// <summary>
        /// Represents an AI-enhanced developer agent designed to assist in various
        /// software development tasks including code development, review, testing,
        /// and documentation.
        /// </summary>
        /// <remarks>
        /// This agent extends the <see cref="BaseAiEnhancedAgent"/> and customizes it
        /// for developer-related capabilities and focus areas. It utilizes AI models
        /// orchestrated by an <see cref="IModelOrchestrator"/> for advanced processing
        /// strategies and functionality specific to software development.
        /// </remarks>
        public AiEnhancedDeveloperAgent(
            IModelOrchestrator modelOrchestrator,
            ILogger<AiEnhancedDeveloperAgent> logger)
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
            AiCapabilities.CanAnalyzeCode = true;
            AiCapabilities.CanGenerateCode = true;
            AiCapabilities.CanAnalyzeTasks = true;
            AiCapabilities.CanProvideSuggestions = true;
            AiCapabilities.CanSolveProblems = true;
            AiCapabilities.PreferredModel = "gpt-4";
            AiCapabilities.ProcessingStrategy = AiProcessingStrategy.Advanced;
        }

        /// <summary>
        /// Processes a developer request asynchronously and returns an appropriate response based on the request type.
        /// </summary>
        /// <param name="request">
        /// An instance of <see cref="AgentRequest"/> representing the incoming developer request.
        /// The type property determines the specific processing logic to be executed.
        /// </param>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> used for propagating notifications that the operation should be canceled.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation,
        /// which upon completion contains an <see cref="AgentResponse"/> object with the response for the processed request.
        /// </returns>
        protected override async Task<AgentResponse> ProcessRequestInternalAsync(AgentRequest request, CancellationToken ct)
        {
            Logger.LogInformation("Processing developer request: {RequestType}", request.Type);

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
                case AgentRequestType.General:
                case AgentRequestType.ArchitectureDesign:
                case AgentRequestType.Collaboration:
                case AgentRequestType.Communication:
                case AgentRequestType.StatusUpdate:
                default:
                    return await HandleGenericRequestAsync(request, ct);
            }
        }

        /// <summary>
        /// Executes custom logic to initialize the agent when it starts.
        /// This method is called as part of the agent's startup process and allows for additional setup or logging.
        /// </summary>
        /// <param name="ct">A cancellation token that notifies the task to cancel its operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task OnStartedAsync(CancellationToken ct)
        {
            Logger.LogInformation("AI-Enhanced Developer Agent started");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Executes necessary operations when the AI-enhanced developer agent is stopped.
        /// This method is invoked during the agent's lifecycle when it transitions to a stopped state.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task OnStoppedAsync(CancellationToken ct)
        {
            Logger.LogInformation("AI-Enhanced Developer Agent stopped");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles the code review process by leveraging AI to analyze the provided code in the request context.
        /// </summary>
        /// <param name="request">The agent request containing details about the code to review, located in the context dictionary under the key "code".</param>
        /// <param name="ct">Cancellation token to signal operation cancellation.</param>
        /// <returns>A task representing the asynchronous operation. The task result is an <see cref="AgentResponse"/> indicating the success of the operation and any resulting content from the code review.</returns>
        private async Task<AgentResponse> HandleCodeReviewAsync(AgentRequest request, CancellationToken ct)
        {
            AgentResponse response;

            // Extract code content from request
            if (request.Context?.ContainsKey("code") == true && request.Context["code"] is string code && !string.IsNullOrWhiteSpace(code))
            {
                var reviewResult = await PerformAiCodeReviewAsync(code, ct);
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

        /// <summary>
        /// Handles the fixing of reported bugs by analyzing the error message and code context,
        /// and generates a suggested solution through AI processing.
        /// </summary>
        /// <param name="request">The agent request containing context data such as the error message and code context.</param>
        /// <param name="ct">A cancellation token to observe while awaiting task completion.</param>
        /// <returns>An AgentResponse object containing a success status and the bug fix suggestion or analysis message.</returns>
        private async Task<AgentResponse> HandleBugFixAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Bug fix analysis completed"
            };

            if (request.Context?.ContainsKey("error_message") != true ||
                (request.Context == null || !request.Context.TryGetValue("code_context", out var value)))
                return response;
            var errorMessage = request.Context["error_message"].ToString();
            var codeContext = value.ToString();
            var fixSuggestion = await GenerateBugFixAsync(errorMessage, codeContext, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = fixSuggestion
            };

            return response;
        }

        /// <summary>
        /// Handles the code generation request by processing the provided requirements and generating corresponding source code.
        /// </summary>
        /// <param name="request">The request object containing context and information required for code generation.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An <see cref="AgentResponse"/> containing the success status and the generated code or a default response if requirements are not provided.</returns>
        private async Task<AgentResponse> HandleCodeGenerationAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Code generation completed"
            };

            if (request.Context == null || !request.Context.TryGetValue("requirements", out var value)) return response;
            var requirements = value.ToString();
            var generatedCode = await GenerateCodeAsync(requirements, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = generatedCode
            };

            return response;
        }

        /// <summary>
        /// Handles the generation of test cases for a given piece of code.
        /// </summary>
        /// <param name="request">The request object containing context information, such as the code to be tested.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="AgentResponse"/> object indicating the success of the operation and containing the generated test code.</returns>
        private async Task<AgentResponse> HandleTestingAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Test generation completed"
            };

            if (request.Context == null || !request.Context.TryGetValue("code_to_test", out var value)) return response;
            var codeToTest = value.ToString();
            var testCode = await GenerateTestsAsync(codeToTest, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = testCode
            };

            return response;
        }

        /// <summary>
        /// Handles the refactoring of the provided code by generating and returning refactoring suggestions.
        /// </summary>
        /// <param name="request">The agent request containing the context and potential code to be refactored.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AgentResponse"/> with the refactored code or a success message.</returns>
        private async Task<AgentResponse> HandleRefactoringAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Refactoring suggestions generated"
            };

            if (request.Context == null || !request.Context.TryGetValue("code_to_refactor", out var value))
                return response;
            var codeToRefactor = value.ToString();
            var refactoredCode = await SuggestRefactoringAsync(codeToRefactor, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = refactoredCode
            };

            return response;
        }

        /// <summary>
        /// Handles the documentation generation request asynchronously.
        /// </summary>
        /// <param name="request">The agent request containing the context and any additional data required for documentation generation.</param>
        /// <param name="ct">The cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="AgentResponse"/> indicating success or failure and containing the generated documentation content.</returns>
        private async Task<AgentResponse> HandleDocumentationAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Documentation generated"
            };

            if (request.Context == null || !request.Context.TryGetValue("code_to_document", out var value))
                return response;
            var codeToDocument = value.ToString();
            var documentation = await GenerateDocumentationAsync(codeToDocument, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = documentation
            };

            return response;
        }

        /// <summary>
        /// Handles a generic request and generates an appropriate response from the developer agent.
        /// </summary>
        /// <param name="request">The agent request containing the type, context, and content to be processed.</param>
        /// <param name="ct">A cancellation token to observe for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation, containing an <see cref="AgentResponse"/>
        /// indicating the success status and associated content of the operation.
        /// </returns>
        private static Task<AgentResponse> HandleGenericRequestAsync(AgentRequest request, CancellationToken ct)
        {
            return Task.FromResult(new AgentResponse
            {
                Success = true,
                Content = $"Developer agent processed request: {request.Content}"
            });
        }

        /// <summary>
        /// Performs an AI-driven comprehensive code review on the provided C# code snippet. The review includes an assessment of code quality,
        /// identification of potential issues, performance considerations, security concerns, best practices, and specific improvement suggestions.
        /// </summary>
        /// <param name="code">The C# code snippet to be reviewed.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to signal cancellation of the operation.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains a structured and detailed review of the code.</returns>
        private async Task<string> PerformAiCodeReviewAsync(string code, CancellationToken ct)
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

            var request = new ModelRequest(0.9, 0.0, 0.0, false)
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        /// Generates a detailed bug fix analysis based on a provided error message and code context.
        /// <param name="errorMessage">The error message describing the issue to be analyzed.</param>
        /// <param name="codeContext">The context or code snippet related to the error for analysis.</param>
        /// <param name="ct">The cancellation token to observe while waiting for the task to complete.</param>
        /// <return>
        /// A string containing the bug fix analysis, including root cause identification, specific fix, explanation, and prevention strategies.
        /// </return>
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

            var request = new ModelRequest(0.9, 0.0, 0.0, false)
            {
                Input = prompt,
                MaxTokens = 1500,
                Temperature = 0.3
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        /// <summary>
        /// Generates C# code based on the provided requirements and returns the generated code as a string.
        /// The generation process includes creating compilable code, XML documentation comments, unit tests,
        /// and usage examples, adhering to best practices and design patterns.
        /// </summary>
        /// <param name="requirements">The textual description of the requirements for the code to be generated.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A string containing the generated code based on the given requirements.</returns>
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

            var request = new ModelRequest(0.9, 0.0, 0.0, false)
            {
                Input = prompt,
                MaxTokens = 3000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        /// <summary>
        /// Generates comprehensive unit tests for the provided C# code using the specified testing framework.
        /// The generated tests include coverage for normal scenarios, edge cases, error conditions, and mock setups where applicable.
        /// </summary>
        /// <param name="codeToTest">The C# code for which unit tests should be generated.</param>
        /// <param name="ct">The cancellation token to observe, allowing the operation to be cancelled.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated test code as a string.</returns>
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

            var request = new ModelRequest(0.9, 0.0, 0.0, false)
            {
                Input = prompt,
                MaxTokens = 2500,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        /// <summary>
        /// Suggests refactoring improvements for a given block of C# code by analyzing it
        /// and providing recommendations to enhance readability, maintainability, and performance.
        /// </summary>
        /// <param name="codeToRefactor">The C# code to be analyzed and refactored.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A string containing refactoring suggestions, identified issues, refactored code examples,
        /// benefits, and potential risks or considerations.</returns>
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

            var request = new ModelRequest(0.9, 0.0, 0.0, false)
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Content;
        }

        /// <summary>
        /// Asynchronously generates XML documentation for the provided C# code based on specified guidelines and standards.
        /// </summary>
        /// <param name="codeToDocument">
        /// The C# code for which the documentation needs to be generated.
        /// </param>
        /// <param name="ct">
        /// The cancellation token to observe while waiting for the asynchronous operation to complete.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the generated documentation as a string.
        /// </returns>
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

            var request = new ModelRequest(0.9, 0.0, 0.0, false)
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