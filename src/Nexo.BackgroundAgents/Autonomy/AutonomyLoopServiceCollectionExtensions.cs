using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.BackgroundAgents.Objectives;

namespace Nexo.BackgroundAgents.Autonomy;

/// <summary>
/// Composes the standing autonomy loop. Lives here rather than beside
/// <c>AddNexoAutonomy</c> because the loop needs both the objective store (this assembly)
/// and the iteration harness (Infrastructure), and Infrastructure cannot see this layer.
///
/// <para>Separate opt-in from <c>AddNexoAutonomy</c>: registering the loop's machinery and
/// running it on a schedule against a live backlog are different decisions, and a host
/// that wants one-shot iterations should not acquire a background sweeper by accident.</para>
/// </summary>
public static class AutonomyLoopServiceCollectionExtensions
{
    /// <summary>
    /// Adds the standing loop. Call after <c>AddNexoAutonomy(configuration)</c>, which supplies
    /// the harness AND the <c>Nexo:Autonomy</c> options the loop obeys: it runs only under
    /// <c>Enabled=true</c>, opens sessions only under <c>UseSandboxSessions=true</c> (falling
    /// back to the options' <c>SessionImage</c>), and reports the <c>HoldAdmission</c> the
    /// harness enforces. Requires an <see cref="IObjectiveStore"/>; one is registered here
    /// only if the host has not already provided its own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Loop settings. Interval defaults to 0, i.e. off.</param>
    public static IServiceCollection AddNexoAutonomyLoop(
        this IServiceCollection services,
        Action<AutonomyLoopSettings>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var settings = new AutonomyLoopSettings();
        configure?.Invoke(settings);
        services.TryAddSingleton(settings);
        services.TryAddSingleton<IObjectiveStore>(_ => new ObjectiveStore());
        services.AddHostedService<AutonomyLoopService>();
        return services;
    }
}
