using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Documentation generation functionality
    /// </summary>
    public partial class AiEnhancedDeveloperAgent
    {
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
            var codeToDocument = value?.ToString() ?? "No code to document provided";
            var documentation = await GenerateDocumentationAsync(codeToDocument, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = documentation
            };

            return response;
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
