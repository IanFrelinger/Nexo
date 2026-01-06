using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Generic agent implementation for domains without specialized agents.
/// 
/// Provides a fallback agent for domains that don't have specialized implementations.
/// Returns placeholder results based on agent specifications.
/// 
/// Used by AgentFactory when no specialized agent matches the domain.
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public sealed class GenericAgent : BaseAgent
{
    public GenericAgent(AgentSpawnSpec spec, ILogger<GenericAgent> logger)
        : base(spec, logger)
    {
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        // Generic initialization - can be overridden by subclasses
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Generic dependency handling
        return Task.CompletedTask;
    }

    protected override Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Generic execution - returns a placeholder result
        // In a real implementation, this would use the model to generate output
        var result = new
        {
            AgentId = Spec.AgentId,
            Domain = Spec.Domain,
            Goal = Spec.Goal,
            Output = $"Generic agent {Spec.AgentId} completed task: {Spec.Goal}"
        };

        return Task.FromResult<object>(result);
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        // Generic shutdown
        return Task.CompletedTask;
    }
}

