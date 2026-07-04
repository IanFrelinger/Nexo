using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Infrastructure.Execution.Agentic;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Sdk.Extensions;
/// <summary>
/// DI registration helpers for NCR core and per-platform policy bindings.
/// </summary>
public static class NodeCapabilityRuntimeServiceCollectionExtensions
{
    /// <summary>Adds node capability runtime core.</summary>
    public static IServiceCollection AddNodeCapabilityRuntimeCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<NodeCapabilityRuntimeOptions>()
            .Bind(configuration.GetSection(NodeCapabilityRuntimeOptions.SectionName));
        services.AddOptions<OllamaBackendOptions>()
            .Bind(configuration.GetSection(OllamaBackendOptions.SectionName));

        services.AddSingleton<IHardwareProfiler, EnvironmentHardwareProfiler>();
        services.AddSingleton<IModelServingBackend, NullModelServingBackend>();
        services.AddSingleton<IModelLifecycleManager, DefaultModelLifecycleManager>();
        services.AddSingleton<ModelScoringService>();
        services.AddSingleton<INodeCapabilityRuntime, NodeCapabilityRuntime>();
        services.AddSingleton<IAgenticBrickEngine, NcrAgenticBrickEngine>();
        services.AddHostedService<NcrStartupHealthService>();
        return services;
    }

    /// <summary>Adds node capability runtime windows.</summary>
    public static IServiceCollection AddNodeCapabilityRuntimeWindows(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return AddDesktopPolicyAndBackend<WindowsPolicy>(services, configuration);
    }

    /// <summary>Adds node capability runtime mac o s.</summary>
    public static IServiceCollection AddNodeCapabilityRuntimeMacOS(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return AddDesktopPolicyAndBackend<MacOsPolicy>(services, configuration);
    }

    /// <summary>Adds node capability runtime linux.</summary>
    public static IServiceCollection AddNodeCapabilityRuntimeLinux(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return AddDesktopPolicyAndBackend<LinuxPolicy>(services, configuration);
    }

    /// <summary>Adds node capability runtimei o s.</summary>
    public static IServiceCollection AddNodeCapabilityRuntimeiOS(
        this IServiceCollection services,
        IConfiguration _)
    {
        return AddPolicy<iOSPolicy>(services);
    }

    /// <summary>Adds node capability runtime android.</summary>
    public static IServiceCollection AddNodeCapabilityRuntimeAndroid(
        this IServiceCollection services,
        IConfiguration _)
    {
        return AddPolicy<AndroidPolicy>(services);
    }

    private static IServiceCollection AddPolicy<TPolicy>(IServiceCollection services)
        where TPolicy : class, IPlatformPolicy, new()
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        services.AddSingleton<IPlatformPolicy, TPolicy>();
        return services;
    }

    private static IServiceCollection AddDesktopPolicyAndBackend<TPolicy>(
        IServiceCollection services,
        IConfiguration configuration)
        where TPolicy : class, IPlatformPolicy, new()
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        AddPolicy<TPolicy>(services);
        services.AddOptions<OllamaBackendOptions>()
            .Bind(configuration.GetSection(OllamaBackendOptions.SectionName));

        services.AddHttpClient(nameof(OllamaModelServingBackend), (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaBackendOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        });

        services.RemoveAll<IModelServingBackend>();
        services.AddSingleton<IModelServingBackend>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(nameof(OllamaModelServingBackend));
            var options = sp.GetRequiredService<IOptions<OllamaBackendOptions>>();
            var metrics = sp.GetService<Nexo.Core.Application.Common.Ports.IMetricsCollector>();
            return new OllamaModelServingBackend(client, options, metrics);
        });
        return services;
    }
}
