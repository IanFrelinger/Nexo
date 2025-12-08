using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Factory for creating specialized agents from AgentSpawnSpec.
/// </summary>
public sealed class AgentFactory
{
    private readonly ILogger<AgentFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AgentFactory(ILogger<AgentFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Creates a BaseAgent instance from an AgentSpawnSpec.
    /// </summary>
    public BaseAgent CreateAgent(AgentSpawnSpec spec)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        _logger.LogInformation("Creating agent {AgentId} for domain {Domain}", spec.AgentId, spec.Domain);

        // Create domain-specific agent based on domain
        var agent = spec.Domain.ToLowerInvariant() switch
        {
            "combat" => CreateCombatAgent(spec),
            "economy" => CreateEconomyAgent(spec),
            "ai" => CreateAIAgent(spec),
            "infrastructure" => CreateInfrastructureAgent(spec),
            "security" => CreateSecurityAgent(spec),
            "gameplay" => CreateGameplayAgent(spec),
            _ => CreateGenericAgent(spec)
        };

        return agent;
    }

    /// <summary>
    /// Creates an AgentContainer wrapping the agent.
    /// </summary>
    public AgentContainer CreateContainer(AgentSpawnSpec spec)
    {
        var agent = CreateAgent(spec);
        var containerLogger = _serviceProvider.GetService(typeof(ILogger<AgentContainer>)) as ILogger<AgentContainer>
            ?? throw new InvalidOperationException("ILogger<AgentContainer> not registered");
        return new AgentContainer(agent, containerLogger);
    }

    private BaseAgent CreateCombatAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<CombatAgent>)) as ILogger<CombatAgent>
            ?? throw new InvalidOperationException("ILogger<CombatAgent> not registered");
        return new CombatAgent(spec, logger);
    }

    private BaseAgent CreateEconomyAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<EconomyAgent>)) as ILogger<EconomyAgent>
            ?? throw new InvalidOperationException("ILogger<EconomyAgent> not registered");
        return new EconomyAgent(spec, logger);
    }

    private BaseAgent CreateAIAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<AIAgent>)) as ILogger<AIAgent>
            ?? throw new InvalidOperationException("ILogger<AIAgent> not registered");
        return new AIAgent(spec, logger);
    }

    private BaseAgent CreateInfrastructureAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<InfrastructureAgent>)) as ILogger<InfrastructureAgent>
            ?? throw new InvalidOperationException("ILogger<InfrastructureAgent> not registered");
        return new InfrastructureAgent(spec, logger);
    }

    private BaseAgent CreateSecurityAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<SecurityAgent>)) as ILogger<SecurityAgent>
            ?? throw new InvalidOperationException("ILogger<SecurityAgent> not registered");
        return new SecurityAgent(spec, logger);
    }

    private BaseAgent CreateGameplayAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<GameplayAgent>)) as ILogger<GameplayAgent>
            ?? throw new InvalidOperationException("ILogger<GameplayAgent> not registered");
        return new GameplayAgent(spec, logger);
    }

    private BaseAgent CreateGenericAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<GenericAgent>)) as ILogger<GenericAgent>
            ?? throw new InvalidOperationException("ILogger<GenericAgent> not registered");
        return new GenericAgent(spec, logger);
    }
}

