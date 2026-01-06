using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Specialized agent for Security domain tasks.
/// 
/// Handles security-related design and implementation:
/// - Authentication and authorization systems
/// - Encryption protocols
/// - Threat modeling
/// - Security best practices
/// 
/// Inherits from BaseDomainAgent for LLM integration and domain-specific prompts.
/// </summary>
public sealed class SecurityAgent : BaseDomainAgent
{
    public SecurityAgent(AgentSpawnSpec spec, ILogger<SecurityAgent> logger, IModel? model = null)
        : base(spec, new LoggerAdapter<SecurityAgent>(logger), model)
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
        return "You are an expert security engineer specializing in application security. Provide detailed designs for authentication, authorization, encryption, threat modeling, and security best practices.";
    }

    protected override object GetMockOutput()
    {
        return new
        {
            AgentId = Spec.AgentId,
            Domain = Spec.Domain,
            Goal = Spec.Goal,
            Output = $"Security agent {Spec.AgentId} completed: {Spec.Goal}",
            SecuritySystem = new
            {
                Authentication = "Designed authentication system",
                Authorization = "Created authorization rules",
                Encryption = "Implemented encryption protocols"
            }
        };
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

