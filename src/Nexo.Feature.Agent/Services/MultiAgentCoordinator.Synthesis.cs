using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Result synthesis and coordination functionality
    /// </summary>
    public partial class MultiAgentCoordinator
    {
        /// <summary>
        /// Synthesizes the results provided by multiple agents for a given collaborative task.
        /// </summary>
        /// <param name="agentResults">A list of results produced by individual agents.</param>
        /// <param name="task">The collaborative task associated with the agent results.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A synthesized output summarizing the agent results in a unified and coherent format.</returns>
        private async Task<string> SynthesizeAgentResultsAsync(List<AgentTaskResult> agentResults, CollaborativeTask task, CancellationToken cancellationToken)
        {
            if (!agentResults.Any(r => r.Success))
            {
                return "No successful agent results to synthesize.";
            }

            var successfulResults = agentResults.Where(r => r.Success).ToList();
            
            // Create synthesis prompt
            var synthesisPrompt = $@"Synthesize the following collaborative task results:

Task: {task.TaskName}
Description: {task.Description}

Agent Results:
{string.Join("\n\n", successfulResults.Select(r => $"Agent: {r.AgentName} ({r.AgentRole})\nResult: {r.Content}"))}

Please provide a comprehensive synthesis that:
1. Identifies common themes and patterns
2. Resolves any conflicts or contradictions
3. Provides a unified, coherent response
4. Highlights the most valuable insights from each agent
5. Suggests next steps or recommendations";

            var synthesisRequest = new ModelRequest
            {
                Input = synthesisPrompt,
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var synthesisResponse = await _modelOrchestrator.ExecuteAsync(synthesisRequest, cancellationToken);
            return synthesisResponse.Response;
        }
    }
}
