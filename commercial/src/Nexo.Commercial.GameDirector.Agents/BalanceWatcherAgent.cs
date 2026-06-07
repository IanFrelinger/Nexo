using Nexo.Abstractions;
using Nexo.Core.Domain.Agents;

namespace GameDirector.Agents;

/// <summary>
/// balance-watcher — registered IAgent; work is performed by <see cref="BalanceWatcherHostedService"/>.
/// </summary>
public sealed class BalanceWatcherAgent : IAgent
{
    public string Name => "balance-watcher";

    public static AgentCard Card => new()
    {
        Id = "balance-watcher",
        Name = "Balance Watcher",
        Domain = "game.balance",
        Description = "Monitors balance sheets for stat drift beyond threshold.",
        Behaviors = ["watch", "analyze", "notify"]
    };

    public Task<AgentActions> ThinkAsync(
        AgentObservation obs,
        IToolbox tools,
        IAgentMemory mem,
        CancellationToken ct) =>
        Task.FromResult(AgentActions.None);
}
