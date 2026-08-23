using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Persistence.Ports;
using Ashlar.Infrastructure.Persistence;

namespace Ashlar.Infrastructure.Persistence.Sdk.Extensions;
/// <summary>
/// DI registration for Ashlar persistence. Use in-memory by default; replace with
/// a database-backed IUnitOfWork from an adapter (e.g. Ashlar.Adapters.Persistence.Sqlite)
/// to avoid database lock-in.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers in-memory persistence: IUnitOfWork (scoped) backed by InMemoryUnitOfWork.
    /// Each scope gets its own store; data does not persist across requests.
    /// For durable storage, use an adapter package and register its IUnitOfWork instead.
    /// </summary>
    public static IServiceCollection AddAshlarPersistence(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
        return services;
    }
}
