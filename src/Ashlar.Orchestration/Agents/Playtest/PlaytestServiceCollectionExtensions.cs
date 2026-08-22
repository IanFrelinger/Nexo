using Microsoft.Extensions.DependencyInjection;

namespace Ashlar.Orchestration.Agents.Playtest;

/// <summary>
/// Registration for the playtest agent layer.
///
/// <para>Lives inside the Playtest tree so it leaves the kernel with it. Once the game
/// layer is its own package this becomes part of that package's public surface, and the
/// kernel has no compile-time knowledge of playtest domains at all.</para>
/// </summary>
public static class PlaytestServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PlaytestAgentProvider"/> so <see cref="AgentFactory"/> routes the
    /// playtest domains to it.
    ///
    /// <para>The agents this provider builds resolve <c>IGameRunner</c> and
    /// <c>ITelemetryStore</c> from the container at creation time. Registering the provider
    /// without also registering those will throw when a playtest agent is actually spawned,
    /// not here — the same contract as before this was extracted.</para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPlaytestAgents(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IDomainAgentProvider, PlaytestAgentProvider>();
        return services;
    }
}
