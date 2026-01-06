using Nexo.Abstractions;

namespace Nexo.Runtime;

/// <summary>
/// Host for executing agents in a simulation step.
/// 
/// Responsibilities:
/// - Executes all registered agents in a single simulation step
/// - Applies policies to approve/reject tool calls
/// - Invokes approved tool calls via IToolbox
/// - Merges action deltas from all agents
/// - Records policy denials in agent memory
/// 
/// Used for agent execution in simulation environments.
/// </summary>
public sealed class AgentHost
{
    private readonly IReadOnlyList<IAgent> _agents;
    private readonly IToolbox _tools;
    private readonly PolicyEngine _policies;

    public AgentHost(IEnumerable<IAgent> agents, IToolbox tools, PolicyEngine policies)
    {
        _agents = agents.ToList();
        _tools = tools;
        _policies = policies;
    }

    public async Task<IActionDelta?> StepAsync(WorldSnapshot s, CancellationToken ct)
    {
        var deltas = new List<IActionDelta>();
        foreach (var agent in _agents)
        {
            var obs = new AgentObservation(s);
            var actions = await agent.ThinkAsync(obs, _tools, _tools.MemoryFor(agent), ct);
            foreach (var call in actions.ToolCalls)
            {
                if (_policies.Approve(call, s, out var reason))
                {
                    var result = await _tools.InvokeAsync(call, s, ct);
                    deltas.Add(_policies.Sign(result.Delta));
                }
                else
                {
                    _tools.MemoryFor(agent).Write(new EventRecord(DateTimeOffset.UtcNow, agent.Name, "policy.denied", $"{call.Id}: {reason}"));
                }
            }
        }
        return deltas.Count == 0 ? null : ActionDelta.Merge(deltas);
    }
}
