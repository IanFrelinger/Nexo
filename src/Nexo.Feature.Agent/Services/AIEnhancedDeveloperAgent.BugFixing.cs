using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Bug fixing and refactoring functionality
    /// </summary>
    public partial class AiEnhancedDeveloperAgent
    {
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
            var errorMessage = request.Context["error_message"]?.ToString() ?? "No error message provided";
            var codeContext = value?.ToString() ?? "No code context provided";
            var fixSuggestion = await GenerateBugFixAsync(errorMessage, codeContext, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = fixSuggestion
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
            var codeToRefactor = value?.ToString() ?? "No code to refactor provided";
            var refactoredCode = await SuggestRefactoringAsync(codeToRefactor, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = refactoredCode
            };

            return response;
        }

        /// <summary>
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

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 1500,
                Temperature = 0.3
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Response;
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

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Response;
        }
    }
}
