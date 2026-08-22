using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.Rollback.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Rollback;

namespace Ashlar.Infrastructure.Rollback.Sdk.Extensions;
/// <summary>
/// DI extensions for rollback infrastructure.
/// </summary>
public static class RollbackServiceCollectionExtensions
{
    /// <summary>
    /// Registers rollback infrastructure: dependency graph, snapshot store, rollback manager.
    /// Requires IAdaptationAuditLog to be registered (e.g. via AddAdaptationInfrastructure).
    /// </summary>
    public static IServiceCollection AddRollbackInfrastructure(this IServiceCollection services, string? snapshotBasePath = null)
    {
        services.AddSingleton<IDependencyGraph, DependencyGraph>();
        services.AddSingleton<ISnapshotStore>(sp =>
        {
            var path = snapshotBasePath ?? Path.Combine(RepoPathResolver.ResolveStateDirectory(), "ashlar-snapshots");
            return new FileSnapshotStore(path);
        });
        services.AddSingleton<IRollbackManager, RollbackManager>();
        return services;
    }
}
