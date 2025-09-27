using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Code review and analysis functionality
    /// </summary>
    public partial class AiEnhancedDeveloperAgent
    {
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

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Response;
        }
    }
}
