using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Application.Agents
{
    /// <summary>
    /// Orchestrates multiple agents to work together on complex tasks
    /// </summary>
    public interface IAgentOrchestrator
    {
        /// <summary>
        /// Register an agent with the orchestrator
        /// </summary>
        Task<AgentResult> RegisterAgentAsync(IAgent agent);

        /// <summary>
        /// Unregister an agent from the orchestrator
        /// </summary>
        Task<AgentResult> UnregisterAgentAsync(AgentId agentId);

        /// <summary>
        /// Get all registered agents
        /// </summary>
        Task<IReadOnlyList<IAgent>> GetRegisteredAgentsAsync();

        /// <summary>
        /// Get agents by capability
        /// </summary>
        Task<IReadOnlyList<IAgent>> GetAgentsByCapabilityAsync(string capabilityName);

        /// <summary>
        /// Get agents by platform
        /// </summary>
        Task<IReadOnlyList<IAgent>> GetAgentsByPlatformAsync(PlatformType platform);

        /// <summary>
        /// Get agents by status
        /// </summary>
        Task<IReadOnlyList<IAgent>> GetAgentsByStatusAsync(AgentStatus status);

        /// <summary>
        /// Find the best agent for a specific task
        /// </summary>
        Task<IAgent?> FindBestAgentForTaskAsync(AgentTask task);

        /// <summary>
        /// Execute a task using the most appropriate agent
        /// </summary>
        Task<AgentResult> ExecuteTaskAsync(AgentTask task);

        /// <summary>
        /// Execute a task using a specific agent
        /// </summary>
        Task<AgentResult> ExecuteTaskAsync(AgentTask task, AgentId agentId);

        /// <summary>
        /// Execute a task using multiple agents in parallel
        /// </summary>
        Task<AgentResult> ExecuteTaskParallelAsync(AgentTask task, IEnumerable<AgentId> agentIds);

        /// <summary>
        /// Execute a task using multiple agents in sequence
        /// </summary>
        Task<AgentResult> ExecuteTaskSequentialAsync(AgentTask task, IEnumerable<AgentId> agentIds);

        /// <summary>
        /// Coordinate multiple agents to work on a complex task
        /// </summary>
        Task<AgentResult> CoordinateAgentsAsync(AgentTask task, IEnumerable<AgentId> agentIds);

        /// <summary>
        /// Broadcast a message to all agents
        /// </summary>
        Task<AgentResult> BroadcastMessageAsync(AgentMessage message);

        /// <summary>
        /// Send a message to specific agents
        /// </summary>
        Task<AgentResult> SendMessageAsync(AgentMessage message, IEnumerable<AgentId> recipientIds);

        /// <summary>
        /// Get agent statistics and metrics
        /// </summary>
        Task<AgentOrchestratorMetrics> GetMetricsAsync();

        /// <summary>
        /// Get agent health status
        /// </summary>
        Task<AgentHealthReport> GetHealthReportAsync();

        /// <summary>
        /// Scale agents based on workload
        /// </summary>
        Task<AgentResult> ScaleAgentsAsync(ScalingStrategy strategy);

        /// <summary>
        /// Balance workload across agents
        /// </summary>
        Task<AgentResult> BalanceWorkloadAsync();

        /// <summary>
        /// Get agent communication network
        /// </summary>
        Task<AgentNetwork> GetAgentNetworkAsync();
    }
}
