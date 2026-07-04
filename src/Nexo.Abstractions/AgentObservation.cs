using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Represents an agent's observation of the world.
///
/// Contains a world snapshot that the agent can use to make decisions.
/// Passed to IAgent.ThinkAsync for agent decision-making.
/// </summary>
/// <param name="Snapshot">The world snapshot the agent is observing.</param>
public sealed record AgentObservation(WorldSnapshot Snapshot);
