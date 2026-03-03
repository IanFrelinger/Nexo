using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions;
using Nexo.Core.Application.Sdk.Ports;
using Nexo.Infrastructure.Adaptation;

namespace Nexo.Hosting.Sdk;

/// <summary>
/// Extension methods for SDK-based component registration.
/// Call AddNexoSdk before AddNexo to register external bricks and agents at runtime.
/// </summary>
public static class NexoSdkServiceCollectionExtensions
{
    /// <summary>
    /// Configures the Nexo SDK builder for runtime registration of bricks and agents.
    /// Call before AddNexo(). Example:
    /// <code>
    /// services.AddNexoSdk(sdk => sdk
    ///     .RegisterBrick&lt;MyBrick&gt;()
    ///     .RegisterAgent&lt;MyAgent&gt;()
    ///     .RegisterAgentCard(myCard));
    /// services.AddNexo();
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the SDK builder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoSdk(
        this IServiceCollection services,
        Action<INexoSdkBuilder> configure)
    {
        var options = new NexoSdkOptions();
        configure(new NexoSdkBuilder(options));

        services.AddSingleton(options);

        if (options.BrickTypes.Count > 0)
            services.Configure<AdaptationBrickOptions>(o => o.AdditionalBrickTypes.AddRange(options.BrickTypes));

        foreach (var agentType in options.AgentTypes)
            services.AddSingleton(typeof(IAgent), agentType);

        return services;
    }
}
