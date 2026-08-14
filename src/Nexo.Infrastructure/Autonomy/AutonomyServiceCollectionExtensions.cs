using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.HotSwap;
using Nexo.Infrastructure.Execution.Sandbox;
using Nexo.Infrastructure.Scaling;

namespace Nexo.Infrastructure.Autonomy;

/// <summary>
/// Composes the autonomous self-extension loop for a host: swap host with its quarantine
/// and watch surfaces, the iteration harness, cluster budget, sandbox session backend, and
/// the leaked-session reaper.
///
/// <para>Host-composed rather than folded into <c>AddNexo</c> deliberately, on the same
/// reasoning as the MCP/A2A surfaces: a kernel consumer that merely references the package
/// must not acquire a loop that can modify the running system. Nothing here runs until
/// <see cref="NexoAutonomyOptions.Enabled"/> is true, and the swap host still refuses
/// non-Tier-0 admissions on its own regardless of how it was registered.</para>
/// </summary>
public static class AutonomyServiceCollectionExtensions
{
    /// <summary>
    /// Registers the autonomy loop. <c>TryAdd</c> throughout so a host may substitute any
    /// piece — its own revocation list, provenance sink, or session backend — by
    /// registering it first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root; binds <c>Nexo:Autonomy</c> when supplied.</param>
    public static IServiceCollection AddNexoAutonomy(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var optionsBuilder = services.AddOptions<NexoAutonomyOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(NexoAutonomyOptions.SectionName));
        optionsBuilder.ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<NexoAutonomyOptions>, ValidateNexoAutonomyOptions>());

        // Authority surfaces: shared singletons, because quarantine and demotion are
        // process-wide facts — two instances would mean two disagreeing truths.
        services.TryAddSingleton<ICertificateRevocationList, InMemoryCertificateRevocationList>();
        services.TryAddSingleton<ILineageAuthority>(sp =>
            new InMemoryLineageAuthority(
                sp.GetRequiredService<IOptions<NexoAutonomyOptions>>().Value.LineageDemotionThreshold));
        services.TryAddSingleton<LoopPauseControl>();
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<NexoAutonomyOptions>>().Value;
            return new ClusterBudget(
                TimeSpan.FromSeconds(options.IterationCeilingSeconds),
                options.ThroughputGuardFactor);
        });

        // The swap host: constructed from options so watch thresholds, retention, and the
        // cadence floor are operator-visible rather than compiled in.
        services.TryAddSingleton<ICertifiedBrickSwapProvenanceSink, LoggingBrickSwapProvenanceSink>();
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<NexoAutonomyOptions>>().Value;
            return new CertifiedBrickHotSwapHost(
                sp.GetRequiredService<ICertifiedBrickSwapProvenanceSink>(),
                sp.GetService<ILogger<CertifiedBrickHotSwapHost>>(),
                hmacKey: null,
                drainTimeout: null,
                revocations: sp.GetRequiredService<ICertificateRevocationList>(),
                retentionWindow: options.RetentionWindow,
                watchThresholds: new WatchThresholds
                {
                    MinInvocations = options.WatchMinInvocations,
                    MaxErrorRateDelta = options.WatchMaxErrorRateDelta,
                    MaxLatencyFactor = options.WatchMaxLatencyFactor,
                },
                lineageAuthority: sp.GetRequiredService<ILineageAuthority>(),
                pauseControl: sp.GetRequiredService<LoopPauseControl>(),
                cadenceFloor: options.CadenceFloorSeconds > 0
                    ? TimeSpan.FromSeconds(options.CadenceFloorSeconds)
                    : null);
        });

        // Sandbox backend + reaper. Registered even when sessions are unused: the reaper
        // is a cleanup backstop for whatever an earlier run leaked, and the harness simply
        // never opens a session when the context supplies no spec.
        services.TryAddSingleton<IProcessCommandRunner, ProcessCommandRunner>();
        // R2.1: no unlogged path from objective to runtime — session lifecycle events get a
        // sink by default, exactly as swap events do.
        services.TryAddSingleton<ISessionProvenanceSink, LoggingSessionProvenanceSink>();
        services.TryAddSingleton<ISandboxedSessionRunner>(sp => new DockerSandboxedSessionRunner(
            sp.GetRequiredService<IProcessCommandRunner>(),
            clock: null,
            logger: sp.GetService<ILogger<DockerSandboxedSessionRunner>>(),
            provenance: sp.GetRequiredService<ISessionProvenanceSink>()));
        services.TryAddSingleton(sp => new DockerSandboxSessionReaper(
            sp.GetRequiredService<IProcessCommandRunner>(),
            clock: null,
            logger: sp.GetService<ILogger<DockerSandboxSessionReaper>>(),
            provenance: sp.GetRequiredService<ISessionProvenanceSink>()));

        // The harness. Resolves the gate through the container rather than constructing
        // one, so a host that substituted its own ICertificationGate keeps it.
        services.TryAddSingleton(sp => new AutonomousIterationHarness(
            sp.GetRequiredService<ICertificationGate>(),
            sp.GetRequiredService<CertifiedBrickHotSwapHost>(),
            sp.GetRequiredService<LoopPauseControl>(),
            sp.GetRequiredService<ILineageAuthority>(),
            sp.GetRequiredService<ISandboxedSessionRunner>(),
            sp.GetRequiredService<ClusterBudget>(),
            sp.GetService<ILogger<AutonomousIterationHarness>>(),
            buildCandidateInSession: sp.GetRequiredService<IOptions<NexoAutonomyOptions>>()
                .Value.BuildCandidateInSession));

        return services;
    }

    /// <summary>
    /// Adds the periodic leaked-session sweep. Separate from <see cref="AddNexoAutonomy"/>
    /// because a CLI process that runs one iteration and exits wants the loop without a
    /// background sweeper, while a long-lived host wants both.
    /// </summary>
    public static IServiceCollection AddNexoAutonomySessionReaper(this IServiceCollection services)
    {
        services.AddHostedService<SandboxSessionReaperService>();
        return services;
    }
}
