using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Infrastructure.Trust;

namespace Ashlar.Infrastructure.Trust.Sdk.Extensions;
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
        services.TryAddSingleton<ITrustPolicyPackRegistry>(sp =>
            new TrustPolicyPackRegistry(Environment.GetEnvironmentVariable("ASHLAR_TRUST_POLICY_PACKS_PATH")));
        services.TryAddSingleton<IAccessBoundary>(sp =>
        {
            var boundary = new AccessBoundary(configPath);
            var auditLog = sp.GetService<Ashlar.Core.Application.Trust.Ports.IDataDecisionAuditLog>();
            var packRegistry = sp.GetService<ITrustPolicyPackRegistry>();
            var activePack = packRegistry?.GetActivePack();
            if (activePack != null)
            {
                var packDefinition = packRegistry?.GetById(activePack.Id);
                if (packDefinition != null)
                    boundary.ApplyPolicyPack(packDefinition);
            }
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

    /// <summary>
    /// Registers ICloudAvailabilityResolver for runtime air-gap resolution.
    /// Sources (priority): ASHLAR_AIRGAP env var, config file, optional network probe.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configPath">Optional config path. Default: ~/.ashlar/config.json.</param>
    /// <param name="enableNetworkProbe">If true, probe api.openai.com when env/config not set.</param>
    public static IServiceCollection AddCloudAvailabilityResolver(
        this IServiceCollection services,
        string? configPath = null,
        bool enableNetworkProbe = false)
    {
        services.TryAddSingleton<ICloudAvailabilityResolver>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CloudAvailabilityResolver>>();
            var path = configPath ?? Environment.GetEnvironmentVariable("ASHLAR_CONFIG_PATH");
            return new CloudAvailabilityResolver(logger, path, enableNetworkProbe);
        });
        return services;
    }
}
