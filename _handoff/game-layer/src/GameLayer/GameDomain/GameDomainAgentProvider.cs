using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.GameDomain.Agents;

namespace Ashlar.Orchestration.GameDomain;

/// <summary>
/// Supplies the game domain agents: combat, economy, gameplay and game AI.
///
/// <para>These were four arms of a hardcoded switch in <see cref="AgentFactory"/>, which is
/// why the kernel could not be built without them. They are all game design agents — the
/// "ai" one is not general-purpose despite its name: its system prompt is "an expert AI/ML
/// engineer specializing in game AI ... NPC behaviors, pathfinding systems". The kernel keeps
/// InfrastructureAgent and SecurityAgent, which are genuinely domain-neutral.</para>
///
/// <para>Note that the kernel still RECOGNISES the AI domain — DomainRecognizer keeps the
/// general-purpose AI vocabulary — it simply has no specialised agent for it and falls back
/// to GenericAgent. Recognising a domain and having a specialist for it are separate
/// concerns, and only the second one is game-specific here.</para>
/// </summary>
public sealed class GameDomainAgentProvider : IDomainAgentProvider
{
    private static readonly string[] Domains = { "combat", "economy", "ai", "gameplay" };

    /// <inheritdoc />
    public bool Handles(string domain) =>
        !string.IsNullOrWhiteSpace(domain) &&
        Array.Exists(Domains, d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public BaseAgent Create(AgentSpawnSpec spec, IAgentCreationContext context)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(context);

        var model = context.ResolveModel(spec);

        return spec.Domain.ToLowerInvariant() switch
        {
            "combat" => new CombatAgent(spec, Logger<CombatAgent>(context), model),
            "economy" => new EconomyAgent(spec, Logger<EconomyAgent>(context), model),
            "ai" => new AIAgent(spec, Logger<AIAgent>(context), model),
            "gameplay" => new GameplayAgent(spec, Logger<GameplayAgent>(context), model),
            _ => throw new ArgumentException($"Unknown game domain: {spec.Domain}")
        };
    }

    /// <summary>
    /// Resolves the concrete logger each agent's constructor asks for. The message matches
    /// the one AgentFactory produced before the move — "ILogger&lt;CombatAgent&gt; not
    /// registered" — so anything asserting on it keeps working.
    /// </summary>
    private static ILogger<T> Logger<T>(IAgentCreationContext context) =>
        context.Services.GetService(typeof(ILogger<T>)) as ILogger<T>
            ?? throw new InvalidOperationException($"ILogger<{typeof(T).Name}> not registered");
}
