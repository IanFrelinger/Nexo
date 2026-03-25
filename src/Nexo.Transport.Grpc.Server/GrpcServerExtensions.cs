using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;

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
        services.TryAddSingleton<IBarrierIdentityResolverPipeline, ExplicitOnlyBarrierIdentityResolverPipeline>();
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

    private sealed class ExplicitOnlyBarrierIdentityResolverPipeline : IBarrierIdentityResolverPipeline
    {
        public ValueTask<BarrierResolutionResult?> ResolveAsync(
            BarrierResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (context is null || string.IsNullOrWhiteSpace(context.ExplicitLevel))
                return default;

            var source = context.Headers.ContainsKey("x-nexo-barrier")
                ? BarrierAuthoritySource.Header
                : BarrierAuthoritySource.Cli;

            var result = new BarrierResolutionResult(
                ResolvedLevel: context.ExplicitLevel,
                ResolverName: "Explicit",
                AuthoritySource: source,
                Detail: "Explicit barrier provided at request boundary.");
            return new ValueTask<BarrierResolutionResult?>(result);
        }
    }
}
