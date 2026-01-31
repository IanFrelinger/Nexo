using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Persistence.Ports;

namespace Nexo.Infrastructure.Persistence;

/// <summary>
/// DI registration for Nexo persistence. Use in-memory by default; replace with
/// a database-backed IUnitOfWork from an adapter (e.g. Nexo.Adapters.Persistence.Sqlite)
/// to avoid database lock-in.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers in-memory persistence: IUnitOfWork (scoped) backed by InMemoryUnitOfWork.
    /// Each scope gets its own store; data does not persist across requests.
    /// For durable storage, use an adapter package and register its IUnitOfWork instead.
    /// </summary>
    public static IServiceCollection AddNexoPersistence(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
        return services;
    }
}
