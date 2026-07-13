using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Skills.AgentFramework.ClassSkills;

namespace Nexo.Skills.AgentFramework;

/// <summary>
/// DI registration for the Microsoft Agent Framework skills adapter.
/// </summary>
public static class AgentFrameworkSkillsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Agent Framework adapter as the Nexo skill catalog implementation.
    /// </summary>
    public static IServiceCollection AddNexoAgentFrameworkSkills(
        this IServiceCollection services,
        Action<NexoSkillsOptions>? configure = null)
    {
        services.AddOptions<NexoSkillsOptions>();
        if (configure != null)
            services.Configure(configure);

        services.TryAddSingleton<AgentFrameworkScriptRunnerBridge>();
        services.TryAddSingleton<NexoActivePolicyPackSkill>();
        services.TryAddSingleton<INexoSkillCatalog, AgentFrameworkSkillCatalog>();
        return services;
    }
}
