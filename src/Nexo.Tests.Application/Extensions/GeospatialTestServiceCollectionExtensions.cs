using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Infrastructure.Execution;

namespace Nexo.Tests.Application.Extensions;

/// <summary>
/// Shared DI setup for geospatial-related tests (BaseFramework, GeospatialE2E).
/// </summary>
public static class GeospatialTestServiceCollectionExtensions
{
    /// <summary>
    /// Adds logging, HTTP clients, and geospatial provider/loop kernel services used by base framework and E2E tests.
    /// </summary>
    public static IServiceCollection AddGeospatialTestServices(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole());
        services.AddHttpClient();
        services.AddHttpClient("geoterrain.srtm");
        services.AddHttpClient("geovector.mapbox");
        services.AddScoped<IProviderFactory, ProviderFactory>();
        services.AddScoped<ILoopKernel, SequentialLoopKernel>();
        return services;
    }
}
