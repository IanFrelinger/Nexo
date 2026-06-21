using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification.Sdk.Extensions;

public static class CertificationServiceCollectionExtensions
{
    public static IServiceCollection AddCertificationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CertificationRecordSigner>();
        services.AddSingleton<ICertificationRecordStore, InMemoryCertificationRecordStore>();
        services.AddSingleton<ICertificationGate, CertificationGate>();
        return services;
    }

    public static IServiceCollection AddCertificationGate(this IServiceCollection services)
    {
        services.AddCertificationInfrastructure();
        services.AddSingleton<CertifiedBrickRegistry>();
        services.AddSingleton<ICertifiedBrickAdmission, CertifiedBrickAdmission>();
        services.AddSingleton<Nexo.Core.Domain.Execution.IBrickRegistry>(sp =>
            sp.GetRequiredService<CertifiedBrickRegistry>());
        return services;
    }
}
