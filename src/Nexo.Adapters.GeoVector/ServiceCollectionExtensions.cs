using Microsoft.Extensions.DependencyInjection;
using Nexo.Adapters.GeoVector.Providers;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVectorProviders(this IServiceCollection services)
    {
        // Default to echo for tests/demos; commands can manually select provider.
        services.AddSingleton<IVectorProvider, EchoVectorProvider>();
        return services;
    }
}

