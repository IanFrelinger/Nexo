using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Agent selection and evaluation functionality
    /// </summary>
    public partial class MultiAgentCoordinator
    {
        /// <summary>
        /// Selects a list of agents for collaboration based on the given request and their evaluated suitability.
        /// </summary>
        /// <param name="request">The collaboration request specifying the requirements and constraints for agent selection.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of agents selected for collaboration.</returns>
        private Task<List<IAiEnhancedAgent>> SelectAgentsForCollaborationAsync(CollaborationRequest request, CancellationToken cancellationToken)
        {
            var candidates = new List<AgentCandidate>();

            lock (_agentsLock)
            {
                candidates.AddRange(from kvp in _registeredAgents let agent = kvp.Value let capabilities = _agentCapabilities[kvp.Key] let score = EvaluateAgentForCollaboration(capabilities, request) select new AgentCandidate { Agent = agent, Score = score });
            }

            // Sort by score and select top agents
            var selectedAgents = candidates
                .OrderByDescending(c => c.Score)
                .Take(request.MaxAgents)
                .Select(c => c.Agent)
                .ToList();

            _logger.LogDebug("Selected {Count} agents for collaboration with scores: {Scores}", 
                selectedAgents.Count, string.Join(", ", candidates.Take(request.MaxAgents).Select(c => $"{c.Agent.Name.Value}:{c.Score:F2}")));

            return Task.FromResult(selectedAgents);
        }

        /// <summary>
        /// Evaluates the suitability of an agent for a collaboration request based on their capabilities, roles, and other criteria.
        /// </summary>
        /// <param name="capabilities">The capability profile of the agent, including supported features and roles.</param>
        /// <param name="request">The requirements of the collaboration request, including roles, capabilities, and AI features.</param>
        /// <returns>A normalized score indicating how well the agent matches the request criteria.</returns>
        private double EvaluateAgentForCollaboration(AgentCapabilityProfile capabilities, CollaborationRequest request)
        {
            var score = 0.0;
            var totalCriteria = 0;

            // Check capability matching
            if (request.RequiredCapabilities.Any())
            {
                var matchingCapabilities = capabilities.Capabilities
                    .Count(cap => request.RequiredCapabilities.Contains(cap));
                score += (double)matchingCapabilities / request.RequiredCapabilities.Count;
                totalCriteria++;
            }

            // Check role matching
            if (request.RequiredRoles.Any())
            {
                if (request.RequiredRoles.Contains(capabilities.AgentRole))
                {
                    score += 1.0;
                }
                totalCriteria++;
            }

            // Check AI capabilities
            if (request.RequireAiCapabilities && capabilities.AiCapabilities.CanAnalyzeTasks)
            {
                score += 1.0;
                totalCriteria++;
            }

            // Check availability (simple heuristic)
            score += 0.5; // Assume agents are available
            totalCriteria++;

            return score / totalCriteria;
        }
    }
}
