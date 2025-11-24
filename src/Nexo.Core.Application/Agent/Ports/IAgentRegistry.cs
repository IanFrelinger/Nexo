using Nexo.Core.Application.Agent.Models;

namespace Nexo.Core.Application.Agent.Ports;

/// <summary>
/// Port for agent registry services.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    Task<IReadOnlyList<AgentMetadata>> GetAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific agent.
    /// </summary>
    Task<AgentMetadata?> GetAgentAsync(string agentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers agents from assemblies.
    /// </summary>
    Task<IReadOnlyList<AgentMetadata>> DiscoverAgentsAsync(CancellationToken cancellationToken = default);
}

