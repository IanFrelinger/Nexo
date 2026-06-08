using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Commercial.Fleet.Infrastructure;

namespace Nexo.Commercial.Fleet.Api;

/// <summary>
/// Commercial fleet director DI and endpoint wiring for operator hosts.
/// </summary>
public static class CommercialFleetHostExtensions
{
    /// <summary>
    /// Registers commercial fleet contracts/infrastructure services used by commercial fleet HTTP endpoints.
    /// </summary>
    public static IServiceCollection AddNexoCommercialFleetDirector(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeKnowledgeReplication = true)
    {
        services.AddNexoFleetDirector(configuration);
        services.AddNexoMeshElasticScheduling(configuration);
        services.AddNexoMeshCheckpointScheduling(configuration);
        if (includeKnowledgeReplication)
            services.AddNexoMeshKnowledgeReplication(configuration);
        services.AddNexoMeshLabWorkerExecutor(configuration);
        return services;
    }

    /// <summary>
    /// Maps commercial <c>/api/mesh</c> fleet/task/knowledge endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapNexoCommercialFleetEndpoints(this IEndpointRouteBuilder app) =>
        app.MapCommercialFleetEndpoints();
}
