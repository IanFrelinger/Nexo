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
    /// AI-Enhanced Architect Agent - Architecture functionality.
    /// </summary>
    public partial class AiEnhancedArchitectAgent
    {
        /// <summary>
        /// Handles the architecture review process by evaluating the provided architecture diagram or system description
        /// and generating a response based on the assessment.
        /// </summary>
        /// <param name="request">The request object containing the context for the architecture review.</param>
        /// <param name="ct">A token to observe while waiting for the task to complete.</param>
        /// <returns>An <see cref="AgentResponse"/> object containing the result of the architecture review.</returns>
        private async Task<AgentResponse> HandleArchitectureReviewAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Architecture review completed"
            };

            if (request.Context?.ContainsKey("architecture_diagram") != true &&
                request.Context?.ContainsKey("system_description") != true) return response;
            var architectureInfo = request.Context.TryGetValue("architecture_diagram", out var value)
                ? value?.ToString() ?? "No architecture diagram provided"
                : request.Context["system_description"]?.ToString() ?? "No system description provided";
                
            var reviewResult = await PerformArchitectureReviewAsync(architectureInfo, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = reviewResult
            };

            return response;
        }

        /// <summary>
        /// Processes a system design request based on the provided requirements and context.
        /// </summary>
        /// <param name="request">The agent request containing requirements and context for the system design.</param>
        /// <param name="ct">A cancellation token to cancel the operation if necessary.</param>
        /// <returns>A <see cref="AgentResponse"/> containing the outcome of the system design process.</returns>
        private async Task<AgentResponse> HandleSystemDesignAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "System design completed"
            };

            if (request.Context == null || !request.Context.TryGetValue("requirements", out var value)) return response;
            var requirements = value?.ToString() ?? "No requirements provided";
            var systemDesign = await GenerateSystemDesignAsync(requirements, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = systemDesign
            };

            return response;
        }

        /// <summary>
        /// Performs a comprehensive architecture review based on the provided architecture information.
        /// Evaluates various aspects of the system architecture such as quality, design patterns, scalability,
        /// security, performance, maintainability, technology stack, risks, and provides improvement recommendations.
        /// </summary>
        /// <param name="architectureInfo">The architectural information such as architecture diagram or system description to be reviewed.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A string containing the detailed results of the architecture review.</returns>
        private async Task<string> PerformArchitectureReviewAsync(string architectureInfo, CancellationToken ct)
        {
            var prompt = $@"Perform a comprehensive architecture review for the following system:

{architectureInfo}

Please provide:
1. Architecture quality assessment
2. Design pattern analysis
3. Scalability considerations
4. Security implications
5. Performance implications
6. Maintainability analysis
7. Technology stack evaluation
8. Risk assessment
9. Improvement recommendations

Focus on enterprise architecture best practices and modern architectural patterns.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 3000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Response;
        }

        /// <summary>
        /// Generates a comprehensive system design architecture based on the provided requirements.
        /// </summary>
        /// <param name="requirements">
        /// The detailed requirements for the system design. This should include all necessary specifications
        /// to generate a robust architectural output.
        /// </param>
        /// <param name="ct">
        /// A CancellationToken that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A string containing the generated system design, which includes high-level architecture,
        /// component breakdown, technology stack recommendations, and other specified design considerations.
        /// </returns>
        private async Task<string> GenerateSystemDesignAsync(string requirements, CancellationToken ct)
        {
            var prompt = $@"Design a comprehensive system architecture based on the following requirements:

{requirements}

Please provide:
1. High-level system architecture
2. Component breakdown
3. Technology stack recommendations
4. Data flow diagrams (textual description)
5. API design considerations
6. Database design considerations
7. Security architecture
8. Scalability strategy
9. Deployment architecture
10. Monitoring and observability strategy

Follow modern architectural patterns and best practices.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 4000,
                Temperature = 0.3
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Response;
        }
    }
}
