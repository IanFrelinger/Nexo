using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Agents.Assets;
using Nexo.Orchestration.Agents.Build;
using Nexo.Orchestration.Agents.Playtest;
using Nexo.Orchestration.Assets.Ports;
using Nexo.Orchestration.Build.Ports;
using Nexo.Orchestration.Playtest.Ports;

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

        // Check if this is an asset generation agent
        if (IsAssetDomain(spec.Domain))
        {
            return CreateAssetAgent(spec);
        }

        // Check if this is a build agent
        if (IsBuildDomain(spec.Domain))
        {
            return CreateBuildAgent(spec);
        }

        // Check if this is a playtest agent
        if (IsPlaytestDomain(spec.Domain))
        {
            return CreatePlaytestAgent(spec);
        }

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
        var model = _serviceProvider.GetService<IModel>();
        return new CombatAgent(spec, logger, model);
    }

    private BaseAgent CreateEconomyAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<EconomyAgent>)) as ILogger<EconomyAgent>
            ?? throw new InvalidOperationException("ILogger<EconomyAgent> not registered");
        var model = _serviceProvider.GetService<IModel>();
        return new EconomyAgent(spec, logger, model);
    }

    private BaseAgent CreateAIAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<AIAgent>)) as ILogger<AIAgent>
            ?? throw new InvalidOperationException("ILogger<AIAgent> not registered");
        var model = _serviceProvider.GetService<IModel>();
        return new AIAgent(spec, logger, model);
    }

    private BaseAgent CreateInfrastructureAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<InfrastructureAgent>)) as ILogger<InfrastructureAgent>
            ?? throw new InvalidOperationException("ILogger<InfrastructureAgent> not registered");
        var model = _serviceProvider.GetService<IModel>();
        return new InfrastructureAgent(spec, logger, model);
    }

    private BaseAgent CreateSecurityAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<SecurityAgent>)) as ILogger<SecurityAgent>
            ?? throw new InvalidOperationException("ILogger<SecurityAgent> not registered");
        var model = _serviceProvider.GetService<IModel>();
        return new SecurityAgent(spec, logger, model);
    }

    private BaseAgent CreateGameplayAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<GameplayAgent>)) as ILogger<GameplayAgent>
            ?? throw new InvalidOperationException("ILogger<GameplayAgent> not registered");
        var model = _serviceProvider.GetService<IModel>();
        return new GameplayAgent(spec, logger, model);
    }

    private BaseAgent CreateGenericAgent(AgentSpawnSpec spec)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<GenericAgent>)) as ILogger<GenericAgent>
            ?? throw new InvalidOperationException("ILogger<GenericAgent> not registered");
        return new GenericAgent(spec, logger);
    }

    private bool IsAssetDomain(string domain)
    {
        var assetDomains = new[] { "image", "audio", "model3d", "3d", "model", "shader", "texture", "animation", "sound", "music" };
        return assetDomains.Contains(domain.ToLowerInvariant());
    }

    private BaseAgent CreateAssetAgent(AgentSpawnSpec spec)
    {
        var model = _serviceProvider.GetService<IModel>();
        var storage = _serviceProvider.GetRequiredService<IAssetStorage>();
        var baseLogger = _serviceProvider.GetService(typeof(ILogger<BaseAgent>)) as ILogger<BaseAgent>
            ?? throw new InvalidOperationException("ILogger<BaseAgent> not registered");

        return spec.Domain.ToLowerInvariant() switch
        {
            "image" or "texture" => new ImageAssetAgent(
                spec,
                _serviceProvider.GetRequiredService<IImageGenerator>(),
                model,
                storage,
                baseLogger),

            "audio" or "sound" or "music" => new AudioAssetAgent(
                spec,
                _serviceProvider.GetRequiredService<IAudioGenerator>(),
                model,
                storage,
                baseLogger),

            "model3d" or "3d" or "model" => new Model3DAssetAgent(
                spec,
                _serviceProvider.GetRequiredService<IModel3DGenerator>(),
                model,
                storage,
                baseLogger),

            _ => throw new ArgumentException($"Unknown asset domain: {spec.Domain}")
        };
    }

    private bool IsBuildDomain(string domain)
    {
        var buildDomains = new[] { "build", "unity", "unreal" };
        return buildDomains.Contains(domain.ToLowerInvariant());
    }

    private BaseAgent CreateBuildAgent(AgentSpawnSpec spec)
    {
        var baseLogger = _serviceProvider.GetService(typeof(ILogger<BaseAgent>)) as ILogger<BaseAgent>
            ?? throw new InvalidOperationException("ILogger<BaseAgent> not registered");
        var storage = _serviceProvider.GetRequiredService<IAssetStorage>();

        return spec.Domain.ToLowerInvariant() switch
        {
            "build" or "unity" => new UnityBuildAgent(
                spec,
                _serviceProvider.GetRequiredService<IBuildTool>(),
                storage,
                baseLogger),

            _ => throw new ArgumentException($"Unknown build domain: {spec.Domain}")
        };
    }

    private bool IsPlaytestDomain(string domain)
    {
        var playtestDomains = new[] { "playtest", "aiplayer", "balance", "telemetry", "feedback" };
        return playtestDomains.Contains(domain.ToLowerInvariant());
    }

    private BaseAgent CreatePlaytestAgent(AgentSpawnSpec spec)
    {
        var baseLogger = _serviceProvider.GetService(typeof(ILogger<BaseAgent>)) as ILogger<BaseAgent>
            ?? throw new InvalidOperationException("ILogger<BaseAgent> not registered");
        var model = _serviceProvider.GetService<IModel>()
            ?? throw new InvalidOperationException("IModel not registered for playtest agents");

        return spec.Domain.ToLowerInvariant() switch
        {
            "aiplayer" or "playtest" => new AIPlayerAgent(
                spec,
                _serviceProvider.GetRequiredService<IGameRunner>(),
                model,
                spec.Constraints.FirstOrDefault(c => c.Type == "Profile")?.Description ?? "balanced",
                baseLogger),

            "balance" => new BalanceAnalyzerAgent(
                spec,
                _serviceProvider.GetRequiredService<ITelemetryStore>(),
                model,
                baseLogger),

            "feedback" => new FeedbackSynthesizerAgent(
                spec,
                model,
                baseLogger),

            _ => throw new ArgumentException($"Unknown playtest domain: {spec.Domain}")
        };
    }
}

