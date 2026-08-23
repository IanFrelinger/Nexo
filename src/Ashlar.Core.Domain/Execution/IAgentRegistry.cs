using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution.Events;

namespace Ashlar.Core.Domain.Execution;

/// <summary>
/// Registry for looking up agent cards by ID.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>Looks up an agent card by id; returns null when not registered.</summary>
    AgentCard? GetAgent(string id);

    /// <summary>Returns all registered agent cards.</summary>
    IReadOnlyList<AgentCard> GetAllAgents();
}
