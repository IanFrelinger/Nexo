using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Agents
{
    /// <summary>
    /// Core interface for all AI agents in the Nexo framework
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Unique identifier for this agent
        /// </summary>
        AgentId Id { get; }

        /// <summary>
        /// Human-readable name of the agent
        /// </summary>
        AgentName Name { get; }

        /// <summary>
        /// Current operational status of the agent
        /// </summary>
        AgentStatus Status { get; }

        /// <summary>
        /// Platform this agent is running on
        /// </summary>
        PlatformType Platform { get; }

        /// <summary>
        /// Agent's capabilities and skills
        /// </summary>
        IReadOnlyList<AgentCapability> Capabilities { get; }

        /// <summary>
        /// Agent's current focus areas
        /// </summary>
        IReadOnlyList<string> FocusAreas { get; }

        /// <summary>
        /// Agent's configuration and settings
        /// </summary>
        IReadOnlyDictionary<string, object> Configuration { get; }

        /// <summary>
        /// Initialize the agent with its environment
        /// </summary>
        Task<AgentResult> InitializeAsync(AgentContext context);

        /// <summary>
        /// Execute a task or command
        /// </summary>
        Task<AgentResult> ExecuteAsync(AgentTask task);

        /// <summary>
        /// Communicate with another agent
        /// </summary>
        Task<AgentResult> CommunicateAsync(IAgent targetAgent, AgentMessage message);

        /// <summary>
        /// Learn from experience and update capabilities
        /// </summary>
        Task<AgentResult> LearnAsync(AgentExperience experience);

        /// <summary>
        /// Shutdown the agent gracefully
        /// </summary>
        Task<AgentResult> ShutdownAsync();

        /// <summary>
        /// Get agent's current state for persistence
        /// </summary>
        AgentState GetState();

        /// <summary>
        /// Restore agent from persisted state
        /// </summary>
        Task<AgentResult> RestoreAsync(AgentState state);
    }
}
