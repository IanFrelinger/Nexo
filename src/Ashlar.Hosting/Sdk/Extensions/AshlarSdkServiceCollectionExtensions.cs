using Microsoft.Extensions.DependencyInjection;
using Ashlar.Abstractions;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Sdk.Ports;

namespace Ashlar.Hosting.Sdk.Extensions;
/// <summary>
/// Extension methods for SDK-based component registration.
/// Call AddAshlarSdk before AddAshlar to register external bricks and agents at runtime.
/// </summary>
public static class AshlarSdkServiceCollectionExtensions
{
    /// <summary>
    /// Configures the Ashlar SDK builder for runtime registration of bricks and agents.
    /// Call before AddAshlar(). Example:
    /// <code>
    /// services.AddAshlarSdk(sdk => sdk
    ///     .RegisterBrick&lt;MyBrick&gt;()
    ///     .RegisterAgent&lt;MyAgent&gt;()
    ///     .RegisterAgentCard(myCard));
    /// services.AddAshlar();
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the SDK builder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAshlarSdk(
        this IServiceCollection services,
        Action<IAshlarSdkBuilder> configure)
    {
        var options = new AshlarSdkOptions();
        configure(new HostAshlarSdkBuilder(options));

        services.AddSingleton(options);

        if (options.BrickTypes.Count > 0)
        {
            services.Configure<AdaptationBrickOptions>(o => o.AdditionalBrickTypes.AddRange(options.BrickTypes));
        }

        foreach (var agentType in options.AgentTypes)
        {
            services.AddSingleton(typeof(IAgent), agentType);
        }

        return services;
    }
}
