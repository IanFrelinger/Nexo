using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Agents;

/// <summary>
/// The kernel services an <see cref="IDomainAgentProvider"/> needs in order to build an
/// agent, without the provider having to reimplement any of them.
///
/// <para>This exists because model resolution is not a simple service lookup.
/// <see cref="ResolveModel"/> wraps the configured model in the orchestration hot-swap
/// wrapper, resolves the runtime spec for the agent id and domain, applies the spec's
/// Ollama override where one is present, and scopes the result to the agent. A provider
/// that tried to do that itself would drift from the kernel the first time any of it
/// changed, and every provider would drift differently.</para>
/// </summary>
public interface IAgentCreationContext
{
    /// <summary>
    /// The service provider agents resolve their own collaborators from — for Playtest,
    /// <c>IGameRunner</c> and <c>ITelemetryStore</c>; for assets, the generators.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// The logger agents are constructed with. Most agents take <c>ILogger&lt;BaseAgent&gt;</c>
    /// rather than a logger of their own concrete type.
    /// </summary>
    ILogger<BaseAgent> BaseLogger { get; }

    /// <summary>
    /// Produces the model instance the agent should run against: hot-swappable, bound to
    /// the resolved runtime spec, and scoped to this agent's id and domain.
    /// </summary>
    /// <param name="spec">The spawn spec the agent is being created from.</param>
    /// <returns>A model ready to hand to an agent constructor.</returns>
    IModel ResolveModel(AgentSpawnSpec spec);
}
