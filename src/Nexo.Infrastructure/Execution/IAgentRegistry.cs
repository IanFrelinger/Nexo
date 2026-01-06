using Nexo.Core.Domain.Agents;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Registry for looking up agent cards by ID.
/// </summary>
public interface IAgentRegistry
{
    AgentCard? GetAgent(string id);
    IReadOnlyList<AgentCard> GetAllAgents();
}

