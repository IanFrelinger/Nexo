using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Application.Interfaces
{
    /// <summary>
    /// Service for managing agents
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// Creates a new agent
        /// </summary>
        Task<AgentResult> CreateAgentAsync(CreateAgentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        Task<IAgent?> GetAgentAsync(AgentId agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all agents
        /// </summary>
        Task<IReadOnlyList<IAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an agent
        /// </summary>
        Task<AgentResult> UpdateAgentAsync(AgentId agentId, UpdateAgentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an agent
        /// </summary>
        Task<AgentResult> DeleteAgentAsync(AgentId agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a task on an agent
        /// </summary>
        Task<AgentResult> ExecuteTaskAsync(AgentId agentId, AgentTask task, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request to create a new agent
    /// </summary>
    public class CreateAgentRequest
    {
        public AgentId Id { get; set; } = null!;
        public AgentName Name { get; set; } = null!;
        public AgentType Type { get; set; }
        public PlatformType Platform { get; set; } = PlatformType.CrossPlatform;
        public IReadOnlyList<AgentCapability> Capabilities { get; set; } = new List<AgentCapability>();
        public IReadOnlyList<string> FocusAreas { get; set; } = new List<string>();
        public IReadOnlyDictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Request to update an agent
    /// </summary>
    public class UpdateAgentRequest
    {
        public AgentName? Name { get; set; }
        public IReadOnlyList<AgentCapability>? Capabilities { get; set; }
        public IReadOnlyList<string>? FocusAreas { get; set; }
        public IReadOnlyDictionary<string, object>? Configuration { get; set; }
    }

    /// <summary>
    /// Agent types
    /// </summary>
    public enum AgentType
    {
        CodeGeneration,
        CodeAnalysis,
        SecurityAnalysis,
        AssemblyAnalysis,
        Documentation,
        Testing,
        Infrastructure,
        Communication,
        DataProcessing,
        Custom
    }
}
