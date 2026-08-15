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
    private static readonly string[] DefaultBarrierLevels = ["public", "internal"];

    /// <summary>
    /// Registers routing transport composition using explicitly-provided local and remote transport types.
    /// Uses TryAdd so host applications can fully override registration.
    /// </summary>
    /// <remarks>
    /// When protocol adapters have contributed <see cref="AgentTransportSchemeRegistration"/>s
    /// (e.g. the A2A transport registering the <c>a2a+</c> endpoint prefix), the remote side is
    /// wrapped in a <see cref="Transport.SchemeDispatchingAgentTransport"/> so multiple remote
    /// protocols coexist behind the single-remote <see cref="RoutingAgentTransport"/> without
    /// any change to <c>EndpointDescriptor</c> or the kernel registrar. With no registrations
    /// the composition is byte-for-byte the previous behavior.
    /// </remarks>
    public static IServiceCollection AddNexoRuntimeTransport<TInProcessTransport, TRemoteTransport>(
        this IServiceCollection services)
        where TInProcessTransport : class, IAgentTransport
        where TRemoteTransport : class, IAgentTransport
    {
        services.TryAddSingleton<TInProcessTransport>();
        services.TryAddSingleton<TRemoteTransport>();
        services.TryAddSingleton<IAgentTransport>(sp =>
        {
            IAgentTransport remote = sp.GetRequiredService<TRemoteTransport>();

            var schemes = sp.GetServices<AgentTransportSchemeRegistration>().ToList();
            if (schemes.Count > 0)
            {
                remote = new Transport.SchemeDispatchingAgentTransport(
                    schemes.Select(s => new KeyValuePair<string, IAgentTransport>(
                        s.EndpointPrefix, s.TransportFactory(sp))),
                    fallback: remote,
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Transport.SchemeDispatchingAgentTransport>>());
            }

            return new RoutingAgentTransport(
                sp.GetRequiredService<TInProcessTransport>(),
                remote,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RoutingAgentTransport>>());
        });
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
            .Configure(options =>
            {
                configuration.GetSection("Nexo:Barriers").Bind(options);
                EnsureBarrierLevels(options);
            });
        services.AddOptions<RoutingOptions>()
            .Configure(options => configuration.GetSection("Nexo:Routing").Bind(options));
        services.AddBarrierAuditSinks(configuration);
        services.AddBarrierIdentityResolvers(configuration);

        services.TryAddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });

        services.TryAddSingleton<IBarrierContextAmbient, BarrierContextAmbient>();
        services.TryAddScoped<IBarrierContextAccessor, ScopedBarrierContextAccessor>();
        services.TryAddSingleton<IBarrierAuditLog, StructuredBarrierAuditLog>();
        services.TryAddSingleton<IEndpointRegistry, InMemoryEndpointRegistry>();
#if NET8_0_OR_GREATER
        services.AddHostedService<EndpointHealthMonitor>();
#endif
        return services;
    }

    private static void EnsureBarrierLevels(BarrierOptions options)
    {
        var levels = options.Levels;
        if (levels is null)
            return;

        var normalized = levels
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        levels.Clear();
        if (normalized.Length == 0)
        {
            foreach (var defaultLevel in DefaultBarrierLevels)
                levels.Add(defaultLevel);
            return;
        }

        foreach (var level in normalized)
            levels.Add(level);
    }

    /// <summary>Registers barrier audit sinks from configuration.</summary>
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

    /// <summary>Registers barrier identity resolvers from configuration.</summary>
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
