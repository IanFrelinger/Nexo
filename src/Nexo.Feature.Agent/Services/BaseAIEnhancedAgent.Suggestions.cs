using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// AI suggestion generation functionality
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent
    {
        /// <summary>
        /// Generates AI-based suggestions for a given sprint task.
        /// </summary>
        /// <param name="task">The <see cref="SprintTask"/> object for which suggestions are to be generated.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="AiSuggestionsResult"/> containing the generated suggestions and related data.</returns>
        public virtual async Task<AiSuggestionsResult> GenerateSuggestionsAsync(SprintTask task, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation("Generating suggestions with AI for agent {AgentName}: {TaskId}", Name.Value, task.Id);

            try
            {
                var prompt = CreateSuggestionsPrompt(task);
                var request = new ModelRequest
                {
                    Input = prompt,
                    MaxTokens = 1500,
                    Temperature = 0.4
                };

                var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
                return ParseSuggestionsResponse(response.Response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating suggestions with AI for agent {AgentName}", Name.Value);
                return new AiSuggestionsResult
                {
                    ConfidenceScore = 0.0
                };
            }
        }
    }
}
