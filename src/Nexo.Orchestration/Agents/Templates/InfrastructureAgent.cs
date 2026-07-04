using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents.Templates;
/// <summary>
/// Specialized agent for Infrastructure domain tasks.
/// 
/// Handles infrastructure-related design and implementation:
/// - System architecture design
/// - Deployment pipelines
/// - Scaling strategies
/// - Infrastructure patterns
/// 
/// Inherits from BaseDomainAgent for LLM integration and domain-specific prompts.
/// </summary>
public sealed class InfrastructureAgent : BaseDomainAgent
{
    public InfrastructureAgent(AgentSpawnSpec spec, ILogger<InfrastructureAgent> logger, IModel? model = null)
        : base(spec, new LoggerAdapter<InfrastructureAgent>(logger), model)
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
        return "You are an expert infrastructure engineer specializing in scalable systems. Provide detailed designs for architecture, deployment pipelines, scaling strategies, and infrastructure patterns.";
    }

    protected override object GetMockOutput()
    {
        return new
        {
            AgentId = Spec.AgentId,
            Domain = Spec.Domain,
            Goal = Spec.Goal,
            Output = $"Infrastructure agent {Spec.AgentId} completed: {Spec.Goal}",
            InfrastructureSystem = new
            {
                Architecture = "Designed system architecture",
                Scalability = "Planned scaling strategy",
                Deployment = "Created deployment pipeline"
            }
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

