using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Infrastructure.Skills;

namespace Nexo.Infrastructure.Skills.Sdk.Extensions;

/// <summary>
/// DI registration for Nexo skill infrastructure services.
/// </summary>
public static class SkillsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Nexo skill policy, runner, approval, audit, and cache services.
    /// </summary>
    public static IServiceCollection AddNexoSkills(this IServiceCollection services)
    {
        services.TryAddSingleton<INexoSkillPolicyEvaluator, SkillPolicyEvaluator>();
        services.TryAddSingleton<INexoScriptSandbox, LocalProcessScriptSandbox>();
        services.TryAddSingleton<INexoScriptRunner, NexoScriptRunner>();
        services.TryAddSingleton<INexoSkillApprovalStore, InMemorySkillApprovalStore>();
        services.TryAddSingleton<INexoSkillApprovalGate, HumanInTheLoopSkillApprovalGate>();
        services.TryAddSingleton<INexoSkillApprovalResolver, SkillApprovalResolver>();
        services.TryAddSingleton<INexoSkillAuditLog, SkillAuditEmitter>();
        services.TryAddSingleton<INexoSkillCacheController, SkillContentHashCacheController>();
        services.TryAddSingleton<INexoSkillExecutionContextFactory, NexoSkillExecutionContextFactory>();
        return services;
    }
}
