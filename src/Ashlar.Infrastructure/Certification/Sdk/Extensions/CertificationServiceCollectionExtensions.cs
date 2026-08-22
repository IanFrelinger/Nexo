using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Autonomy;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification.Composition;
using Ashlar.Infrastructure.Certification.HotSwap;

namespace Ashlar.Infrastructure.Certification.Sdk.Extensions;

/// <summary>Registers certification infrastructure services (gate, stores, composition pipeline).</summary>
public static class CertificationServiceCollectionExtensions
{
    /// <summary>Adds certification infrastructure.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="recordStorePath">
    /// Directory for DURABLE certification records. When supplied, admissions survive the
    /// process; when null the store is in-memory, as before.
    /// </param>
    /// <remarks>
    /// The in-memory default means a certification granted in one process is invisible to
    /// the next, so nothing can ever be admitted from prior state. That is fine for a
    /// single-process host that certifies as it goes, and fatal for the CLI, where each
    /// invocation is a fresh process: <c>ashlar adapt --store-path ...</c> could never find
    /// an admitted brick, whatever had been certified earlier.
    ///
    /// Durability does not weaken admission. Records are written signed and re-verified on
    /// load by <see cref="FileCertificationRecordStore"/>, and the signature covers the
    /// record's ContentHash, so a mutated record reads as uncertified rather than as
    /// admitted.
    /// </remarks>
    public static IServiceCollection AddCertificationInfrastructure(
        this IServiceCollection services,
        string? recordStorePath = null)
    {
        services.AddSingleton<CertificationRecordSigner>();

        if (string.IsNullOrWhiteSpace(recordStorePath))
        {
            services.AddSingleton<ICertificationRecordStore, InMemoryCertificationRecordStore>();
        }
        else
        {
            var directory = recordStorePath;
            services.AddSingleton<ICertificationRecordStore>(sp =>
                new FileCertificationRecordStore(
                    directory,
                    sp.GetRequiredService<CertificationRecordSigner>()));
        }

        services.AddSingleton<ICertificationGate, CertificationGate>();
        services.AddSingleton<CompositionCertificationRecordSigner>();
        services.AddSingleton<ICompositionCertificationRecordStore, InMemoryCompositionCertificationRecordStore>();
        services.AddSingleton<ICompositionCertificationGate, CompositionCertificationGate>();
        return services;
    }

    /// <summary>Adds certification gate.</summary>
    public static IServiceCollection AddCertificationGate(this IServiceCollection services)
    {
        services.AddCertificationInfrastructure();
        services.AddSingleton<CertifiedBrickRegistry>();
        services.AddSingleton<ICertifiedBrickAdmission, CertifiedBrickAdmission>();
        services.AddSingleton<CertifiedCompositionRegistry>();
        services.AddSingleton<ICertifiedCompositionAdmission, CertifiedCompositionAdmission>();
        services.AddSingleton<Ashlar.Core.Domain.Execution.IBrickRegistry>(sp =>
            sp.GetRequiredService<CertifiedBrickRegistry>());
        return services;
    }

    /// <summary>
    /// Adds the certified brick hot-swap host: verify-at-load, one collectible load
    /// context per generation, fail-closed swaps, provenance events per swap outcome.
    /// TryAdd throughout so hosts can substitute their own provenance sink.
    /// </summary>
    [Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
    public static IServiceCollection AddCertifiedBrickHotSwapHost(this IServiceCollection services)
    {
        services.TryAddSingleton<ICertifiedBrickSwapProvenanceSink, LoggingBrickSwapProvenanceSink>();
        services.TryAddSingleton(sp => new CertifiedBrickHotSwapHost(
            sp.GetRequiredService<ICertifiedBrickSwapProvenanceSink>(),
            sp.GetService<ILogger<CertifiedBrickHotSwapHost>>()));
        return services;
    }
}
