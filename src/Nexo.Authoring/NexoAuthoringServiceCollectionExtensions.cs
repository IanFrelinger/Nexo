using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Hosting.Sdk.Extensions;

namespace Nexo.Authoring;

/// <summary>
/// Stable registration helpers for code-authored Nexo bricks and agent profiles.
/// </summary>
public static class NexoAuthoringServiceCollectionExtensions
{
    /// <summary>
    /// Registers a code-authored brick with the Nexo host SDK.
    /// Call before <c>AddNexo()</c>.
    /// </summary>
    public static IServiceCollection AddNexoBrick<TBrick>(this IServiceCollection services)
        where TBrick : DomainBrick
    {
        return services.AddNexoSdk(sdk => sdk.RegisterBrick<TBrick>());
    }

    /// <summary>
    /// Ensures <see cref="IAgentProfileRegistry"/> is available and registers
    /// the fixed <see cref="GenerativeArtifactBrick"/>.
    /// </summary>
    public static IServiceCollection AddNexoGenerativeAgents(this IServiceCollection services)
    {
        services.TryAddSingleton<IAgentProfileRegistry>(sp =>
            new AgentProfileRegistry(sp.GetServices<IAgentProfileSource>(), sp));
        services.TryAddSingleton<GenerativeArtifactBrick>();
        return services.AddNexoBrick<GenerativeArtifactBrick>();
    }

    /// <summary>
    /// Registers an agent profile factory (mirrors <see cref="AddNexoBrick{TBrick}"/>).
    /// Profiles are loaded when the registry is constructed — zero edits to the fixed engine.
    /// </summary>
    public static IServiceCollection AddNexoAgentProfile<TProfile>(this IServiceCollection services)
        where TProfile : class, IAgentProfileFactory
    {
        services.TryAddSingleton<IAgentProfileRegistry>(sp =>
            new AgentProfileRegistry(sp.GetServices<IAgentProfileSource>(), sp));
        services.AddSingleton<TProfile>();
        services.AddSingleton<IAgentProfileSource>(sp => new FactoryProfileSource<TProfile>(sp));
        return services;
    }

    private sealed class FactoryProfileSource<TProfile> : IAgentProfileSource
        where TProfile : class, IAgentProfileFactory
    {
        public FactoryProfileSource(IServiceProvider _) { }
        public AgentProfile Create(IServiceProvider services) =>
            services.GetRequiredService<TProfile>().Create(services);
    }
}

/// <summary>Factory that builds an <see cref="AgentProfile"/> from DI.</summary>
public interface IAgentProfileFactory
{
    /// <summary>Creates the profile instance.</summary>
    AgentProfile Create(IServiceProvider services);
}
