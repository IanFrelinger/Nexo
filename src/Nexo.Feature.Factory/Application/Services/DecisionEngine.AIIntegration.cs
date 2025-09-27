using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.Factory.Application.Services
{
    /// <summary>
    /// AI integration functionality for model calls and response handling
    /// </summary>
    public partial class DecisionEngine
    {
        private async Task<string> CallAIAsync(string prompt, CancellationToken cancellationToken)
        {
            var request = new Nexo.Feature.AI.Models.ModelRequest
            {
                Input = prompt,
                SystemPrompt = "You are a software architecture expert. Analyze the given feature specification and provide structured analysis results. Return only valid JSON without any additional text or explanations.",
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            return response.Response;
        }
    }
}
