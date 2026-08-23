using Ashlar.Abstractions;

namespace Ashlar.Runtime;

/// <summary>
/// Host for executing agents in a simulation step.
///
/// Responsibilities:
/// - Executes all registered agents in a single simulation step
/// - Validates all tool calls before executing any (no partial application)
/// - Applies policies to approve/reject tool calls
/// - Invokes approved tool calls via IToolbox
/// - Merges action deltas from all agents
/// - Records policy denials in agent memory
/// - Invokes chain rejection callback when a chain is rejected
///
/// Used for agent execution in simulation environments.
/// </summary>
public sealed class AgentHost
{
    private readonly IReadOnlyList<IAgent> _agents;
    private readonly IToolbox _tools;
    private readonly PolicyEngine _policies;
    private readonly ChainRejectionCallback? _onChainRejected;

    /// <summary>Creates a host with the given agents, toolbox, policies, and optional chain rejection callback.</summary>
    public AgentHost(IEnumerable<IAgent> agents, IToolbox tools, PolicyEngine policies, ChainRejectionCallback? onChainRejected = null)
    {
        _agents = agents.ToList();
        _tools = tools;
        _policies = policies;
        _onChainRejected = onChainRejected;
    }

    /// <summary>Runs one simulation step: think, validate, invoke approved tools, and merge action deltas.</summary>
    public async Task<IActionDelta?> StepAsync(WorldSnapshot s, CancellationToken ct)
    {
        var deltas = new List<IActionDelta>();
        foreach (var agent in _agents)
        {
            var obs = new AgentObservation(s);
            var actions = await agent.ThinkAsync(obs, _tools, _tools.MemoryFor(agent), ct);
            var calls = actions.ToolCalls.ToList();
            if (calls.Count == 0) continue;

            // Validate all before executing any — no partial application
            var rejectedAt = -1;
            var rejectedReason = "";
            for (var i = 0; i < calls.Count; i++)
            {
                if (!_policies.Approve(calls[i], s, out var reason))
                {
                    rejectedAt = i;
                    rejectedReason = reason;
                    break;
                }
            }

            if (rejectedAt >= 0)
            {
                _tools.MemoryFor(agent).Write(new EventRecord(DateTimeOffset.UtcNow, agent.Name, "policy.denied", $"{calls[rejectedAt].Id}: {rejectedReason}"));
                _onChainRejected?.Invoke(calls, rejectedAt, rejectedReason);
                continue;
            }

            foreach (var call in calls)
            {
                var result = await _tools.InvokeAsync(call, s, ct);
                deltas.Add(_policies.Sign(result.Delta));
            }
        }
        return deltas.Count == 0 ? null : ActionDelta.Merge(deltas);
    }
}
