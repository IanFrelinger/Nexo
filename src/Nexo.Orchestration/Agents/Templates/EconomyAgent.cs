using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents.Templates;
/// <summary>
/// Specialized agent for Economy domain tasks.
/// 
/// Handles economy-related design and implementation:
/// - Currency systems
/// - Trading mechanics
/// - Pricing models
/// - Economic balance
/// 
/// Inherits from BaseDomainAgent for LLM integration and domain-specific prompts.
/// </summary>
public sealed class EconomyAgent : BaseDomainAgent
{
    public EconomyAgent(AgentSpawnSpec spec, ILogger<EconomyAgent> logger, IModel? model = null)
        : base(spec, new LoggerAdapter<EconomyAgent>(logger), model)
    {
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        // Economy-specific initialization
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Economy agents may depend on combat for item values, infrastructure for trading systems
        return Task.CompletedTask;
    }

    protected override string GetSystemPrompt()
    {
        return "You are an expert game economist specializing in virtual economies. Provide detailed designs for currency systems, trading mechanics, pricing models, and economic balance.";
    }

    protected override object GetMockOutput()
    {
        return new
        {
            AgentId = Spec.AgentId,
            Domain = Spec.Domain,
            Goal = Spec.Goal,
            Output = $"Economy agent {Spec.AgentId} completed: {Spec.Goal}",
            EconomySystem = new
            {
                Currency = "Designed currency system",
                Trading = "Created trading mechanics",
                Pricing = "Established pricing models"
            }
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

