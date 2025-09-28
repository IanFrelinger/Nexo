using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Application.Agents
{
    /// <summary>
    /// Factory for creating and managing agents
    /// </summary>
    public interface IAgentFactory
    {
        /// <summary>
        /// Create a new agent of the specified type
        /// </summary>
        Task<IAgent> CreateAgentAsync(AgentType agentType, AgentId id, AgentName name, PlatformType platform);

        /// <summary>
        /// Create a new agent with custom configuration
        /// </summary>
        Task<IAgent> CreateAgentAsync(AgentType agentType, AgentId id, AgentName name, PlatformType platform, AgentConfiguration configuration);

        /// <summary>
        /// Create a new agent from a template
        /// </summary>
        Task<IAgent> CreateAgentFromTemplateAsync(AgentTemplate template, AgentId id, AgentName name, PlatformType platform);

        /// <summary>
        /// Create a new agent by cloning an existing agent
        /// </summary>
        Task<IAgent> CloneAgentAsync(IAgent sourceAgent, AgentId newId, AgentName newName);

        /// <summary>
        /// Create a new agent from persisted state
        /// </summary>
        Task<IAgent> CreateAgentFromStateAsync(AgentState state);

        /// <summary>
        /// Get available agent types
        /// </summary>
        Task<IReadOnlyList<AgentType>> GetAvailableAgentTypesAsync();

        /// <summary>
        /// Get agent templates
        /// </summary>
        Task<IReadOnlyList<AgentTemplate>> GetAgentTemplatesAsync();

        /// <summary>
        /// Validate agent configuration
        /// </summary>
        Task<AgentResult> ValidateAgentConfigurationAsync(AgentConfiguration configuration);

        /// <summary>
        /// Get agent creation options for a platform
        /// </summary>
        Task<AgentCreationOptions> GetAgentCreationOptionsAsync(PlatformType platform);
    }
}
