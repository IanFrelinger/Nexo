using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Infrastructure.Execution.Agentic;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;

namespace Nexo.Infrastructure.NodeCapabilityRuntime;

/// <summary>
/// DI registration helpers for NCR core and per-platform policy bindings.
/// </summary>
public static class NodeCapabilityRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddNodeCapabilityRuntimeCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<NodeCapabilityRuntimeOptions>()
            .Bind(configuration.GetSection(NodeCapabilityRuntimeOptions.SectionName));

        services.AddSingleton<IHardwareProfiler, EnvironmentHardwareProfiler>();
        services.AddSingleton<IModelServingBackend, NullModelServingBackend>();
        services.AddSingleton<IModelLifecycleManager, DefaultModelLifecycleManager>();
        services.AddSingleton<ModelScoringService>();
        services.AddSingleton<INodeCapabilityRuntime, NodeCapabilityRuntime>();
        services.AddSingleton<IAgenticBrickEngine, NcrAgenticBrickEngine>();
        return services;
    }

    public static IServiceCollection AddNodeCapabilityRuntimeWindows(
        this IServiceCollection services,
        IConfiguration _)
    {
        return AddPolicy<WindowsPolicy>(services);
    }

    public static IServiceCollection AddNodeCapabilityRuntimeMacOS(
        this IServiceCollection services,
        IConfiguration _)
    {
        return AddPolicy<MacOsPolicy>(services);
    }

    public static IServiceCollection AddNodeCapabilityRuntimeLinux(
        this IServiceCollection services,
        IConfiguration _)
    {
        return AddPolicy<LinuxPolicy>(services);
    }

    public static IServiceCollection AddNodeCapabilityRuntimeiOS(
        this IServiceCollection services,
        IConfiguration _)
    {
        return AddPolicy<iOSPolicy>(services);
    }

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
}
