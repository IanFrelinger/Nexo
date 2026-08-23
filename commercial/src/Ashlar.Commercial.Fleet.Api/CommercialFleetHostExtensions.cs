using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Commercial.Fleet.Infrastructure;

namespace Ashlar.Commercial.Fleet.Api;

/// <summary>
/// Commercial fleet director DI and endpoint wiring for operator hosts.
/// </summary>
public static class CommercialFleetHostExtensions
{
    /// <summary>
    /// Registers commercial fleet contracts/infrastructure services used by commercial fleet HTTP endpoints.
    /// </summary>
    public static IServiceCollection AddAshlarCommercialFleetDirector(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeKnowledgeReplication = true)
    {
        services.AddAshlarFleetDirector(configuration);
        services.AddAshlarMeshElasticScheduling(configuration);
        services.AddAshlarMeshCheckpointScheduling(configuration);
        if (includeKnowledgeReplication)
            services.AddAshlarMeshKnowledgeReplication(configuration);
        services.AddAshlarMeshLabWorkerExecutor(configuration);
        return services;
    }

    /// <summary>
    /// Maps commercial <c>/api/mesh</c> fleet/task/knowledge endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAshlarCommercialFleetEndpoints(this IEndpointRouteBuilder app) =>
        app.MapCommercialFleetEndpoints();
}
