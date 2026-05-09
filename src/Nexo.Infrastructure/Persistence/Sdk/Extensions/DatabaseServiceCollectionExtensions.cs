using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Abstractions.Database;
using Nexo.Infrastructure.Persistence;

namespace Nexo.Infrastructure.Sdk.Persistence;

/// <summary>
/// DI registration for isolated database provisioning.
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDatabaseProvisioner"/> backed by Postgres (Docker ephemeral, new DB, or new schema).
    /// </summary>
    public static IServiceCollection AddPostgresIsolatedDatabaseProvisioner(this IServiceCollection services)
    {
        services.TryAddSingleton<IDatabaseProvisioner, PostgresDatabaseProvisioner>();
        return services;
    }
}
