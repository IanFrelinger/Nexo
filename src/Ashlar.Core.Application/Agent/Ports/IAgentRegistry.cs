using Ashlar.Core.Application.Agent.Models;

namespace Ashlar.Core.Application.Agent.Ports;

/// <summary>
/// Port for agent registry services.
/// 
/// Defines the contract for discovering and querying available agents:
/// - Get all registered agents
/// - Get metadata for a specific agent
/// - Discover agents from assemblies
/// 
/// Implementations (AgentRegistryAdapter) provide agent discovery and metadata.
/// Used by CLI commands to list available agents.
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

