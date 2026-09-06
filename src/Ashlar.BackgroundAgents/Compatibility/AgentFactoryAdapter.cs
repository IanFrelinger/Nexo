using Ashlar.Abstractions;
using Ashlar.Core.Application.Orchestration.Ports;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.BackgroundAgents.Compatibility;

/// <summary>
/// Temporary compatibility adapter bridging IAgentCreator port and legacy AgentFactory.
/// Accepts AgentSpawnSpecDto (new DTO from Application ports) and forwards to AgentFactory (Orchestration layer).
/// </summary>
/// <remarks>
/// TODO: Delete after application/ CLI migrates to IAgentCreator injection.
/// This adapter exists only to prevent chicken-egg compile breaks during the src/-only port migration.
/// </remarks>
[Obsolete("Temporary compatibility shim for CLI migration. Use IAgentCreator directly. Will be removed after application/ updates.")]
public sealed class AgentFactoryAdapter : IAgentCreator
{
    private readonly AgentFactory _agentFactory;

    public AgentFactoryAdapter(AgentFactory agentFactory)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
    }

    /// <inheritdoc />
    public IAgent CreateAgent(AgentSpawnSpecDto spec)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        // Convert DTO → full AgentSpawnSpec
        var fullSpec = new AgentSpawnSpec
        {
            AgentId = spec.AgentId,
            Name = spec.Name,
            Domain = spec.Domain,
            Goal = spec.Goal,
            Description = spec.Description,
            Dependencies = spec.Dependencies,
            OllamaModel = spec.OllamaModel
        };

        return _agentFactory.CreateAgent(fullSpec);
    }
}
