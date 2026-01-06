using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;

namespace Nexo.Core.Application.Agent.UseCases.ListAgents;

/// <summary>
/// MediatR handler for listing available agents.
/// 
/// Responsibilities:
/// - Retrieves agent metadata from IAgentRegistry
/// - Returns list of available agents with their capabilities
/// - Logs agent discovery operations
/// 
/// Part of the Application layer's use case pattern, following CQRS principles.
/// </summary>
public class ListAgentsHandler : IRequestHandler<ListAgentsQuery, IReadOnlyList<AgentMetadata>>
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<ListAgentsHandler> _logger;

    public ListAgentsHandler(
        IAgentRegistry agentRegistry,
        ILogger<ListAgentsHandler> logger)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<AgentMetadata>> Handle(
        ListAgentsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing available agents");

        var agents = await _agentRegistry.GetAgentsAsync(cancellationToken);

        _logger.LogInformation("Found {Count} agent(s)", agents.Count);

        return agents;
    }
}

