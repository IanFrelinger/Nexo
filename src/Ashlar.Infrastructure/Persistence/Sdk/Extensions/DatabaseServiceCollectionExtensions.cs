using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ashlar.Abstractions.Database;
using Ashlar.Infrastructure.Persistence;

namespace Ashlar.Infrastructure.Persistence.Sdk.Extensions;
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
