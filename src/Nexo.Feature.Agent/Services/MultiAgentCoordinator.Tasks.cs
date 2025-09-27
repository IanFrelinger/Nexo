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
    /// Collaborative task execution functionality
    /// </summary>
    public partial class MultiAgentCoordinator
    {
        /// <summary>
        /// Executes a collaborative task with multiple agents.
        /// </summary>
        /// <param name="task">The collaborative task to be executed, which includes its name, description, and required details.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the collaboration result with details of the execution.</returns>
        public async Task<CollaborationResult> ExecuteCollaborativeTaskAsync(CollaborativeTask task, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing collaborative task: {TaskName}", task.TaskName);

            try
            {
                var session = await CreateCollaborationSessionAsync(new CollaborationRequest
                {
                    SessionName = $"Task_{task.TaskName}_{DateTime.UtcNow.Ticks}",
                    Description = task.Description,
                    SessionType = CollaborationSessionType.TaskExecution,
                    RequiredCapabilities = task.RequiredCapabilities,
                    RequiredRoles = task.RequiredRoles,
                    Configuration = task.Configuration
                }, cancellationToken);

                var result = new CollaborationResult
                {
                    SessionId = session.SessionId,
                    TaskName = task.TaskName,
                    Success = true,
                    AgentResults = new List<AgentTaskResult>(),
                    CollaborationMetrics = new CollaborationMetrics(0.0, 0.0m)
                };

                var startTime = DateTime.UtcNow;

                // Execute task with participating agents
                foreach (var agent in session.ParticipatingAgents)
                {
                    var agentResult = await ExecuteTaskWithAgentAsync(agent, task, cancellationToken);
                    result.AgentResults.Add(agentResult);
                }

                // Coordinate and synthesize results
                var synthesisResult = await SynthesizeAgentResultsAsync(result.AgentResults, task, cancellationToken);
                result.SynthesizedResult = synthesisResult;
                result.CollaborationMetrics.TotalProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                result.CollaborationMetrics.AgentCount = session.ParticipatingAgents.Count;

                // Update session status
                session.Status = CollaborationSessionStatus.Completed;
                session.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Completed collaborative task {TaskName} with {AgentCount} agents", 
                    task.TaskName, session.ParticipatingAgents.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing collaborative task: {TaskName}", task.TaskName);
                return new CollaborationResult
                {
                    TaskName = task.TaskName,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Executes a collaborative task with a specified agent asynchronously.
        /// </summary>
        /// <param name="agent">The agent responsible for processing the task.</param>
        /// <param name="task">The collaborative task to be executed by the agent.</param>
        /// <param name="cancellationToken">A token to cancel the execution process if needed.</param>
        /// <returns>Returns the result of the task execution containing the agent's response and metadata.</returns>
        private async Task<AgentTaskResult> ExecuteTaskWithAgentAsync(IAiEnhancedAgent agent, CollaborativeTask task, CancellationToken cancellationToken)
        {
            try
            {
                var aiRequest = new AiEnhancedAgentRequest
                {
                    Type = AgentRequestType.Collaboration,
                    Content = task.Description,
                    UseAi = true,
                    AiContext = new Dictionary<string, object>
                    {
                        ["taskName"] = task.TaskName,
                        ["taskType"] = task.TaskType,
                        ["collaborationMode"] = "true",
                        ["agentRole"] = agent.Role.Value
                    }
                };

                var response = await agent.ProcessAiRequestAsync(aiRequest, cancellationToken);

                return new AgentTaskResult
                {
                    AgentId = agent.Id.Value,
                    AgentName = agent.Name.Value,
                    AgentRole = agent.Role.Value,
                    Success = response.Success,
                    Content = response.Content,
                    ProcessingTimeMs = response.AiProcessingTimeMs,
                    AiWasUsed = response.AiWasUsed,
                    AiModelUsed = response.AiModelUsed,
                    ConfidenceScore = response.AiConfidenceScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing task with agent {AgentName}", agent.Name.Value);
                return new AgentTaskResult
                {
                    AgentId = agent.Id.Value,
                    AgentName = agent.Name.Value,
                    AgentRole = agent.Role.Value,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
