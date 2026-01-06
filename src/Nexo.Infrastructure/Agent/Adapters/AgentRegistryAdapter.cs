using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;
using System.Reflection;

namespace Nexo.Infrastructure.Agent.Adapters;

/// <summary>
/// Infrastructure adapter for agent registry.
/// 
/// Implements IAgentRegistry port from Application layer. Provides:
/// - Agent discovery from service provider
/// - Agent metadata extraction (name, description, capabilities, parameters)
/// - Agent lookup by name
/// 
/// Part of the Infrastructure layer, implementing the hexagonal architecture pattern.
/// </summary>
public class AgentRegistryAdapter : IAgentRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentRegistryAdapter> _logger;

    public AgentRegistryAdapter(
        IServiceProvider serviceProvider,
        ILogger<AgentRegistryAdapter> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<AgentMetadata>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = new List<AgentMetadata>();

        // Get agents from service provider
        var registeredAgents = _serviceProvider.GetServices<IAgent>();
        foreach (var agent in registeredAgents)
        {
            agents.Add(new AgentMetadata
            {
                Name = agent.Name,
                Description = GetAgentDescription(agent),
                Capabilities = GetAgentCapabilities(agent),
                Parameters = GetAgentParameters(agent)
            });
        }

        _logger.LogInformation("Found {Count} registered agent(s)", agents.Count);
        return Task.FromResult<IReadOnlyList<AgentMetadata>>(agents);
    }

    public Task<AgentMetadata?> GetAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        var agents = _serviceProvider.GetServices<IAgent>();
        var agent = agents.FirstOrDefault(a => 
            a.Name.Equals(agentName, StringComparison.OrdinalIgnoreCase));

        if (agent == null)
        {
            return Task.FromResult<AgentMetadata?>(null);
        }

        return Task.FromResult<AgentMetadata?>(new AgentMetadata
        {
            Name = agent.Name,
            Description = GetAgentDescription(agent),
            Capabilities = GetAgentCapabilities(agent),
            Parameters = GetAgentParameters(agent)
        });
    }

    public Task<IReadOnlyList<AgentMetadata>> DiscoverAgentsAsync(CancellationToken cancellationToken = default)
    {
        // For now, return registered agents
        // Future: scan assemblies for IAgent implementations
        return GetAgentsAsync(cancellationToken);
    }

    private static string? GetAgentDescription(IAgent agent)
    {
        var type = agent.GetType();
        var xmlDoc = type.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return xmlDoc?.Description ?? type.Name;
    }

    private static IReadOnlyList<string> GetAgentCapabilities(IAgent agent)
    {
        // Extract capabilities from agent type or attributes
        // Placeholder implementation
        return new[] { "Execution", "ToolInvocation" };
    }

    private static IReadOnlyDictionary<string, string> GetAgentParameters(IAgent agent)
    {
        // Extract parameters from agent type
        // Placeholder implementation
        return new Dictionary<string, string>();
    }
}

