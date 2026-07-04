using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Certification.Composition;

namespace Nexo.Infrastructure.Certification.Sdk.Extensions;

/// <summary>Registers certification infrastructure services (gate, stores, composition pipeline).</summary>
public static class CertificationServiceCollectionExtensions
{
    /// <summary>Adds certification infrastructure.</summary>
    public static IServiceCollection AddCertificationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CertificationRecordSigner>();
        services.AddSingleton<ICertificationRecordStore, InMemoryCertificationRecordStore>();
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
        services.AddSingleton<Nexo.Core.Domain.Execution.IBrickRegistry>(sp =>
            sp.GetRequiredService<CertifiedBrickRegistry>());
        return services;
    }
}
