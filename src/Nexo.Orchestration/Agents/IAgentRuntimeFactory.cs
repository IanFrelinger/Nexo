using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Spawns orchestration agents as <see cref="IAgentHandle"/> instances.
/// </summary>
public interface IAgentRuntimeFactory
{
    /// <summary>
    /// Creates a handle for the agent described by <paramref name="spec"/>.
    /// </summary>
    IAgentHandle Spawn(AgentSpawnSpec spec);
}
