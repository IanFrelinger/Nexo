using Microsoft.Extensions.DependencyInjection;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect;

namespace Ashlar.Orchestration.GameDomain;

/// <summary>
/// Registration for the game domain layer.
///
/// <para>Lives inside the GameDomain tree so it leaves the kernel with it. Once the game
/// layer is its own package this becomes part of that package's public surface.</para>
/// </summary>
public static class GameDomainServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="GameDomainPatternProvider"/> so <see cref="DomainRecognizer"/>
    /// recognises Combat, Economy, Gameplay and game-AI vocabulary.
    ///
    /// <para>Without this, a request mentioning "combat" or "loot" simply matches no domain.
    /// That is not a failure — it is a kernel with no game layer installed correctly
    /// declining to guess at vocabulary it does not own. Domain recognition feeds RAG
    /// similarity scoring and architect hints, both of which degrade rather than break.</para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddGameDomainPatterns(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IDomainPatternProvider, GameDomainPatternProvider>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="GameDomainAgentProvider"/> so <see cref="AgentFactory"/> routes
    /// the combat, economy, ai and gameplay domains to the game design agents.
    ///
    /// <para>Without this, those domains fall through to GenericAgent. Note the kernel still
    /// recognises the AI domain — only the specialised game-AI agent lives here.</para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddGameDomainAgents(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IDomainAgentProvider, GameDomainAgentProvider>();
        return services;
    }

    /// <summary>
    /// Registers the whole game domain layer — recognition patterns and agents.
    /// The one call an application installing the game layer is expected to make.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddGameDomain(this IServiceCollection services) =>
        services.AddGameDomainPatterns().AddGameDomainAgents();
}
