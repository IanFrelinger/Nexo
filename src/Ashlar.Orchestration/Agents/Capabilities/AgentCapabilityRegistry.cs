using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Agents.Capabilities;

/// <summary>
/// Registry for agent capabilities and discovery.
/// 
/// Responsibilities:
/// - Registers agent capabilities (capabilities, modalities, languages)
/// - Finds agents by capability type
/// - Finds agents by supported input modality
/// - Provides capability lookup and discovery
/// 
/// Thread-safe implementation using concurrent collections.
/// Used for agent discovery and capability-based routing.
/// </summary>
public sealed class AgentCapabilityRegistry
{
    private readonly ConcurrentDictionary<string, AgentCapability> _capabilities = new();

    /// <summary>
    /// Registers an agent capability.
    /// </summary>
    public void Register(string agentId, AgentCapability capability)
    {
        _capabilities[agentId] = capability;
    }

    /// <summary>
    /// Gets capabilities for an agent.
    /// </summary>
    public AgentCapability? GetCapability(string agentId)
    {
        return _capabilities.TryGetValue(agentId, out var capability) ? capability : null;
    }

    /// <summary>
    /// Finds agents with a specific capability.
    /// </summary>
    public IEnumerable<string> FindAgentsWithCapability(string capabilityType)
    {
        return _capabilities
            .Where(kvp => kvp.Value.Capabilities.Contains(capabilityType, StringComparer.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Finds agents that support a specific input modality.
    /// </summary>
    public IEnumerable<string> FindAgentsWithModality(string modality)
    {
        return _capabilities
            .Where(kvp => kvp.Value.SupportedModalities.Contains(modality, StringComparer.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets all registered capabilities.
    /// </summary>
    public IReadOnlyDictionary<string, AgentCapability> GetAllCapabilities()
    {
        return _capabilities;
    }
}
