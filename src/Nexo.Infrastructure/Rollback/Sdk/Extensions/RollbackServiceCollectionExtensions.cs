using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Rollback.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Rollback;

namespace Nexo.Infrastructure.Sdk.Rollback;

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
            var path = snapshotBasePath ?? Path.Combine(RepoPathResolver.FindRepoRoot(), "nexo-snapshots");
            return new FileSnapshotStore(path);
        });
        services.AddSingleton<IRollbackManager, RollbackManager>();
        return services;
    }
}
