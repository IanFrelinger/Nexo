using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Transport.Grpc.Server;

/// <summary>
/// Host registration helpers for Nexo gRPC transport server.
/// </summary>
public static class GrpcServerExtensions
{
    /// <summary>
    /// Registers Nexo gRPC transport server services.
    /// </summary>
    public static IServiceCollection AddNexoGrpcServer(this IServiceCollection services)
    {
        services.AddGrpc();
        return services;
    }

    /// <summary>
    /// Maps Nexo gRPC transport endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapNexoGrpcServer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<AgentTransportServiceImpl>();
        return endpoints;
    }
}
