using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;

namespace Nexo.Core.Application.Agent.UseCases.ListAgents;

/// <summary>
/// Handler for listing available agents.
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

