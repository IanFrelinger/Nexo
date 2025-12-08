using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Specialized agent for AI domain tasks.
/// </summary>
public sealed class AIAgent : BaseDomainAgent
{
    public AIAgent(AgentSpawnSpec spec, ILogger<AIAgent> logger, IModel? model = null)
        : base(spec, new LoggerAdapter<AIAgent>(logger), model)
    {
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        // AI-specific initialization
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // AI agents may depend on combat for enemy behaviors, gameplay for NPC interactions
        return Task.CompletedTask;
    }

    protected override string GetSystemPrompt()
    {
        return "You are an expert AI/ML engineer specializing in game AI. Provide detailed designs for NPC behaviors, pathfinding systems, decision-making algorithms, and AI architecture.";
    }

    protected override object GetMockOutput()
    {
        return new
        {
            AgentId = Spec.AgentId,
            Domain = Spec.Domain,
            Goal = Spec.Goal,
            Output = $"AI agent {Spec.AgentId} completed: {Spec.Goal}",
            AISystem = new
            {
                Behaviors = "Designed AI behaviors",
                Pathfinding = "Created pathfinding system",
                DecisionMaking = "Implemented decision trees"
            }
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

