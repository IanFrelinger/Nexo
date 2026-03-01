using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Infrastructure.Trust;

/// <summary>
/// DI extensions for Trust &amp; Information Architecture.
/// </summary>
public static class TrustServiceCollectionExtensions
{
    /// <summary>
    /// Registers IUserKnowledgeLogStore. Uses in-memory store by default.
    /// For durable storage, call with a database path. Uses LiteDB (pure managed) for cross-platform support.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="dbPath">Optional database file path. If null, uses in-memory store.</param>
    public static IServiceCollection AddUserKnowledgeLog(this IServiceCollection services, string? dbPath = null)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
            services.TryAddSingleton<IUserKnowledgeLogStore, InMemoryUserKnowledgeLogStore>();
        else
            services.TryAddSingleton<IUserKnowledgeLogStore>(_ => new LiteDbUserKnowledgeLogStore(dbPath));
        return services;
    }

    /// <summary>
    /// Registers IAccessBoundary and IObservationGate (Phase 3: Access Boundary).
    /// Observation pipelines should call IObservationGate.ShouldObserve before persisting data.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configPath">Optional path to persist boundary config (JSON). If null, in-memory only.</param>
    public static IServiceCollection AddAccessBoundary(this IServiceCollection services, string? configPath = null)
    {
        services.TryAddSingleton<IAccessBoundary>(sp =>
        {
            var boundary = new AccessBoundary(configPath);
            var auditLog = sp.GetService<Nexo.Core.Application.Trust.Ports.IDataDecisionAuditLog>();
            if (auditLog != null)
                boundary.BoundaryChanged += evt => auditLog.LogBoundaryChange(evt);
            return boundary;
        });
        services.TryAddSingleton<IObservationGate>(sp =>
        {
            var boundary = sp.GetRequiredService<IAccessBoundary>();
            return new ObservationGate(boundary);
        });
        return services;
    }
}
