using Nexo.Core.Domain.Agents;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// In-memory registry for agent cards.
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, AgentCard> _agents = new();
    
    public AgentRegistry(IEnumerable<AgentCard> agents)
    {
        foreach (var agent in agents)
        {
            _agents[agent.Id] = agent;
        }
    }
    
    public AgentCard? GetAgent(string id)
    {
        return _agents.TryGetValue(id, out var agent) ? agent : null;
    }
    
    public IReadOnlyList<AgentCard> GetAllAgents()
    {
        return _agents.Values.ToList();
    }
}

