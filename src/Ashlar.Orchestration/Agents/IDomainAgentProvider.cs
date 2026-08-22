using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Agents;

/// <summary>
/// Supplies agents for a set of domains that the kernel itself knows nothing about.
///
/// <para><see cref="AgentFactory"/> asks every registered provider, in registration order,
/// whether it <see cref="Handles"/> a domain and hands the first claimant the job. Providers
/// are consulted BEFORE the kernel's own built-in domains, so a package can also override a
/// built-in if it needs to.</para>
///
/// <para>This is the seam that lets game-specific agents live outside the kernel. Before it
/// existed, <c>AgentFactory.CreateAgent</c> switched on hardcoded domain strings
/// (<c>"aiplayer"</c>, <c>"balance"</c>, <c>"feedback"</c>, ...) and constructed concrete
/// game types directly, which meant the kernel could not be built without the game code
/// present — extracting it produced <c>CS0234: namespace 'Playtest' does not exist</c>.</para>
///
/// <para>Note the <see cref="IAgentCreationContext"/> parameter. An earlier sketch of this
/// interface was <c>BaseAgent Create(AgentSpawnSpec spec)</c>, which cannot work: building an
/// agent requires the wrapped, runtime-spec-resolved, agent-scoped model that only the kernel
/// knows how to construct. The context supplies it rather than making every provider
/// reimplement it.</para>
/// </summary>
public interface IDomainAgentProvider
{
    /// <summary>
    /// Whether this provider creates agents for the given domain. Implementations should
    /// compare case-insensitively; callers pass the domain exactly as it appeared on the spec.
    /// </summary>
    /// <param name="domain">The domain from the spawn spec, e.g. "aiplayer".</param>
    /// <returns>True if <see cref="Create"/> can build an agent for this domain.</returns>
    bool Handles(string domain);

    /// <summary>
    /// Builds the agent for a domain this provider claimed.
    /// </summary>
    /// <param name="spec">The spawn spec.</param>
    /// <param name="context">Kernel services, including model resolution.</param>
    /// <returns>The constructed agent.</returns>
    /// <exception cref="ArgumentException">
    /// If the domain was claimed by <see cref="Handles"/> but cannot actually be built.
    /// </exception>
    BaseAgent Create(AgentSpawnSpec spec, IAgentCreationContext context);
}
