using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// In-memory registry for agent cards.
/// </summary>
public class AgentRegistry : Ashlar.Core.Domain.Execution.IAgentRegistry
{
    private readonly Dictionary<string, AgentCard> _agents = new();
    
    /// <summary>Initializes a new agent registry.</summary>
    public AgentRegistry(IEnumerable<AgentCard> agents)
    {
        foreach (var agent in agents)
        {
            _agents[agent.Id] = agent;
        }
    }
    
    /// <summary>Gets agent.</summary>
    public AgentCard? GetAgent(string id)
    {
        return _agents.TryGetValue(id, out var agent) ? agent : null;
    }
    
    /// <summary>Gets all agents.</summary>
    public IReadOnlyList<AgentCard> GetAllAgents()
    {
        return _agents.Values.ToList();
    }
}

