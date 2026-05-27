using Nexo.Abstractions;
using Nexo.Core.Domain.Agents;

namespace GameDirector.Agents;

public sealed class MapValidatorAgent : IAgent
{
    public string Name => "map-validator";

    public static AgentCard Card => new()
    {
        Id = "map-validator",
        Name = "Map Validator",
        Domain = "game.map",
        Description = "Validates .mapconfig files on change and periodic full scan.",
        Behaviors = ["validate", "report"]
    };

    public Task<AgentActions> ThinkAsync(
        AgentObservation obs,
        IToolbox tools,
        IAgentMemory mem,
        CancellationToken ct) =>
        Task.FromResult(AgentActions.None);
}
