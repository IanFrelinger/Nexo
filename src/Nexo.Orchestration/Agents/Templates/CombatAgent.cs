using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents.Templates;
/// <summary>
/// Specialized agent for Combat domain tasks.
/// 
/// Handles combat-related design and implementation:
/// - Weapon systems
/// - Damage mechanics
/// - Combat balance
/// - Actionable combat system designs
/// 
/// Inherits from BaseDomainAgent for LLM integration and domain-specific prompts.
/// </summary>
public sealed class CombatAgent : BaseDomainAgent
{
    public CombatAgent(
        AgentSpawnSpec spec,
        ILogger<CombatAgent> logger,
        IModel? model = null)
        : base(spec, new LoggerAdapter<CombatAgent>(logger), model)
    {
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        // Combat-specific initialization
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Combat agents may depend on other systems (e.g., economy for weapon pricing)
        return Task.CompletedTask;
    }

    protected override string GetSystemPrompt()
    {
        return "You are an expert game designer specializing in combat systems. Provide detailed, actionable designs with clear mechanics, balance considerations, and implementation guidance.";
    }

    protected override object GetMockOutput()
    {
        return new
        {
            AgentId = Spec.AgentId,
            Domain = Spec.Domain,
            Goal = Spec.Goal,
            Output = $"Combat agent {Spec.AgentId} completed: {Spec.Goal}",
            CombatSystem = new
            {
                Weapons = "Designed weapon system",
                Damage = "Calculated damage mechanics",
                Balance = "Balanced combat interactions"
            }
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        // Combat-specific cleanup
        return Task.CompletedTask;
    }
}

