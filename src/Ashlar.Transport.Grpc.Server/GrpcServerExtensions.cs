using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Barriers.Identity;

namespace Ashlar.Transport.Grpc.Server;

/// <summary>
/// Host registration helpers for Ashlar gRPC transport server.
/// </summary>
public static class GrpcServerExtensions
{
    /// <summary>
    /// Registers Ashlar gRPC transport server services.
    /// </summary>
    public static IServiceCollection AddAshlarGrpcServer(this IServiceCollection services)
    {
        services.AddGrpc();
        services.TryAddSingleton<IBarrierIdentityResolverPipeline, ExplicitOnlyBarrierIdentityResolverPipeline>();
        return services;
    }

    /// <summary>
    /// Maps Ashlar gRPC transport endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAshlarGrpcServer(this IEndpointRouteBuilder endpoints)
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

            var source = context.Headers.ContainsKey("x-ashlar-barrier")
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
