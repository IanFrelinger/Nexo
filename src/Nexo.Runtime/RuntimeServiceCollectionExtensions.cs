using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.Runtime.Barriers;
using Nexo.Runtime.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Nexo.Runtime.Barriers.Sinks;
using Nexo.Runtime.Routing;
using Nexo.Runtime.Transport;

namespace Nexo.Runtime;

/// <summary>
/// Runtime-layer DI helpers for transport routing.
/// </summary>
public static class RuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers routing transport composition using explicitly-provided local and remote transport types.
    /// Uses TryAdd so host applications can fully override registration.
    /// </summary>
    public static IServiceCollection AddNexoRuntimeTransport<TInProcessTransport, TRemoteTransport>(
        this IServiceCollection services)
        where TInProcessTransport : class, IAgentTransport
        where TRemoteTransport : class, IAgentTransport
    {
        services.TryAddSingleton<TInProcessTransport>();
        services.TryAddSingleton<TRemoteTransport>();
        services.TryAddSingleton<IAgentTransport>(sp =>
            new RoutingAgentTransport(
                sp.GetRequiredService<TInProcessTransport>(),
                sp.GetRequiredService<TRemoteTransport>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RoutingAgentTransport>>()));
        return services;
    }

    /// <summary>
    /// Registers routing registry/options/monitor services from configuration.
    /// </summary>
    public static IServiceCollection AddNexoRuntimeRouting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddOptions<RoutingOptions>()
            .Configure(options => configuration.GetSection("Nexo:Routing").Bind(options));
        services.AddBarrierAuditSinks(configuration);
        services.AddBarrierIdentityResolvers(configuration);

        services.TryAddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });

        services.TryAddScoped<IBarrierContextAccessor, ScopedBarrierContextAccessor>();
        services.TryAddSingleton<IBarrierAuditLog, StructuredBarrierAuditLog>();
        services.TryAddSingleton<IEndpointRegistry, InMemoryEndpointRegistry>();
#if NET8_0_OR_GREATER
        services.AddHostedService<EndpointHealthMonitor>();
#endif
        return services;
    }

    public static IServiceCollection AddBarrierAuditSinks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var sinkNames = configuration.GetSection("Nexo:Audit:Sinks").Get<string[]>() ?? [];
        var normalizedSinks = sinkNames
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedSinks.Length == 0)
        {
            services.TryAddSingleton<IBarrierAuditSink, NoOpBarrierAuditSink>();
            return services;
        }

        foreach (var sinkName in normalizedSinks)
        {
            switch (sinkName)
            {
                case "File":
#if NET8_0_OR_GREATER
                    services.AddOptions<FileBarrierAuditSinkOptions>()
                        .Configure(options => configuration.GetSection("Nexo:Audit:File").Bind(options));
                    services.TryAddSingleton<FileBarrierAuditSink>(sp =>
                        new FileBarrierAuditSink(
                            sp.GetRequiredService<IOptions<FileBarrierAuditSinkOptions>>().Value,
                            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileBarrierAuditSink>>()));
                    services.AddSingleton<IBarrierAuditSink>(sp => sp.GetRequiredService<FileBarrierAuditSink>());
                    services.AddHostedService<FileBarrierAuditSinkLifetime>();
                    break;
#else
                    throw new InvalidOperationException("The File audit sink requires a NET8+ runtime target.");
#endif
                case "StructuredLog":
                    services.AddOptions<StructuredLogBarrierAuditSinkOptions>()
                        .Configure(options => configuration.GetSection("Nexo:Audit:StructuredLog").Bind(options));
                    services.TryAddSingleton<StructuredLogBarrierAuditSink>(sp =>
                        new StructuredLogBarrierAuditSink(
                            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<StructuredLogBarrierAuditSink>>(),
                            sp.GetRequiredService<IOptions<StructuredLogBarrierAuditSinkOptions>>().Value));
                    services.AddSingleton<IBarrierAuditSink>(sp => sp.GetRequiredService<StructuredLogBarrierAuditSink>());
                    break;
                case "NoOp":
                    services.TryAddSingleton<NoOpBarrierAuditSink>();
                    services.AddSingleton<IBarrierAuditSink>(sp => sp.GetRequiredService<NoOpBarrierAuditSink>());
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown audit sink: '{sinkName}'. Valid values: File, StructuredLog, NoOp");
            }
        }

        return services;
    }

    public static IServiceCollection AddBarrierIdentityResolvers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<BarrierIdentityResolverOptions>()
            .Configure(options => configuration.GetSection("Nexo:Identity").Bind(options));
        services.AddOptions<PkiCertificateResolverOptions>()
            .Configure(options => configuration.GetSection("Nexo:Identity:PkiCertificate").Bind(options));
        services.AddOptions<JwtClaimResolverOptions>()
            .Configure(options => configuration.GetSection("Nexo:Identity:JwtClaim").Bind(options));
        services.AddOptions<ApiKeyResolverOptions>()
            .Configure(options => configuration.GetSection("Nexo:Identity:ApiKey").Bind(options));

        services.TryAddSingleton<IBarrierIdentityResolverPipeline, DefaultBarrierIdentityResolverPipeline>();
        services.TryAddSingleton<PkiCertificateBarrierResolver>(sp =>
            new PkiCertificateBarrierResolver(
                sp.GetRequiredService<IOptions<PkiCertificateResolverOptions>>().Value,
                sp.GetRequiredService<BarrierHierarchy>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PkiCertificateBarrierResolver>>()));
        services.TryAddSingleton<JwtClaimBarrierResolver>(sp =>
            new JwtClaimBarrierResolver(
                sp.GetRequiredService<IOptions<JwtClaimResolverOptions>>().Value,
                sp.GetRequiredService<BarrierHierarchy>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JwtClaimBarrierResolver>>()));
        services.TryAddSingleton<ApiKeyBarrierResolver>(sp =>
            new ApiKeyBarrierResolver(
                sp.GetRequiredService<IOptions<ApiKeyResolverOptions>>().Value,
                sp.GetRequiredService<BarrierHierarchy>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiKeyBarrierResolver>>()));

        var resolverPriority = configuration.GetSection("Nexo:Identity:ResolverPriority").Get<string[]>() ?? [];
        foreach (var configuredName in resolverPriority)
        {
            var resolverName = configuredName?.Trim();
            if (string.IsNullOrWhiteSpace(resolverName))
                continue;

            switch (resolverName)
            {
                case "PkiCertificate":
                    services.AddSingleton<IBarrierIdentityResolver>(sp => sp.GetRequiredService<PkiCertificateBarrierResolver>());
                    break;
                case "JwtClaim":
                    services.AddSingleton<IBarrierIdentityResolver>(sp => sp.GetRequiredService<JwtClaimBarrierResolver>());
                    break;
                case "ApiKey":
                    services.AddSingleton<IBarrierIdentityResolver>(sp => sp.GetRequiredService<ApiKeyBarrierResolver>());
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown barrier identity resolver: '{resolverName}'. Valid values: PkiCertificate, JwtClaim, ApiKey");
            }
        }

        return services;
    }
}
