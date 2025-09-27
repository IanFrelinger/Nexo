using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// AI model communication and response handling functionality
    /// </summary>
    public sealed partial class DomainAnalysisAgent
    {
        private async Task<string> CallAIAsync(string prompt, CancellationToken cancellationToken)
        {
            var request = new Nexo.Feature.AI.Models.ModelRequest
            {
                Input = prompt,
                SystemPrompt = "You are a domain analysis expert. Analyze the given description and extract structured domain information. Return only valid JSON without any additional text or explanations.",
                MaxTokens = 4000,
                Temperature = 0.3
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            return response.Response;
        }
    }
}
