using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Core.Domain.Execution;

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
