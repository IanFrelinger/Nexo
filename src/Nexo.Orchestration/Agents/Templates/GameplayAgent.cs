using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Specialized agent for Gameplay domain tasks.
/// </summary>
public sealed class GameplayAgent : BaseDomainAgent
{
    public GameplayAgent(AgentSpawnSpec spec, ILogger<GameplayAgent> logger, IModel? model = null)
        : base(spec, new LoggerAdapter<GameplayAgent>(logger), model)
    {
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override string GetSystemPrompt()
    {
        return "You are an expert game designer specializing in gameplay mechanics. Provide detailed designs for game mechanics, progression systems, player experience, and feature implementation.";
    }

    protected override object GetMockOutput()
    {
        return new
        {
            AgentId = Spec.AgentId,
            Domain = Spec.Domain,
            Goal = Spec.Goal,
            Output = $"Gameplay agent {Spec.AgentId} completed: {Spec.Goal}",
            GameplaySystem = new
            {
                Mechanics = "Designed gameplay mechanics",
                Progression = "Created progression systems",
                Features = "Implemented game features"
            }
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

