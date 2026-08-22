using Microsoft.Extensions.DependencyInjection;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Playtest.Ports;

namespace Ashlar.Orchestration.Agents.Playtest;

/// <summary>
/// Supplies the playtest agents — AI player, balance analyser and feedback synthesiser.
///
/// <para>This is game-specific and leaves the kernel with the rest of the Playtest tree
/// (see <c>scripts/handoff/extract-game-layer.sh</c>). It exists so that
/// <see cref="AgentFactory"/> no longer needs to know these domains: previously
/// <c>CreateAgent</c> switched on the same four strings and constructed these types
/// directly, so the kernel could not compile without the game code present.</para>
///
/// <para>Register with <c>services.AddPlaytestAgents()</c>. If it is not registered the
/// playtest domains fall through to the kernel's generic agent, which is the correct
/// behaviour for a kernel that has no game layer installed.</para>
/// </summary>
public sealed class PlaytestAgentProvider : IDomainAgentProvider
{
    private static readonly string[] Domains =
        { "playtest", "aiplayer", "balance", "telemetry", "feedback" };

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
        var baseLogger = context.BaseLogger;

        // NB: "telemetry" is listed in Domains above and so is claimed by Handles, but has
        // no case below and therefore throws. That is exactly what the code this replaced
        // did — IsPlaytestDomain included "telemetry" while CreatePlaytestAgent had no arm
        // for it — and it is preserved rather than quietly repaired, because changing it
        // here would mix a behaviour change into a mechanical extraction. It looks like a
        // latent bug: the domain is advertised as supported and then rejected at runtime.
        return spec.Domain.ToLowerInvariant() switch
        {
            "aiplayer" or "playtest" => new AIPlayerAgent(
                spec,
                context.Services.GetRequiredService<IGameRunner>(),
                model,
                spec.Constraints.FirstOrDefault(c => c.Type == "Profile")?.Description ?? "balanced",
                baseLogger),

            "balance" => new BalanceAnalyzerAgent(
                spec,
                context.Services.GetRequiredService<ITelemetryStore>(),
                model,
                baseLogger),

            "feedback" => new FeedbackSynthesizerAgent(
                spec,
                model,
                baseLogger),

            // No paramName, matching the original exactly. Adding nameof(spec) would be
            // better practice but appends " (Parameter 'spec')" to Message, and the point
            // of this move is that no behaviour change hides inside it — including the
            // text of an exception someone may be matching on.
            _ => throw new ArgumentException($"Unknown playtest domain: {spec.Domain}")
        };
    }
}
