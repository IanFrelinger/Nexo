using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Certification.Composition;

namespace Nexo.Infrastructure.Certification.Sdk.Extensions;

public static class CertificationServiceCollectionExtensions
{
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
