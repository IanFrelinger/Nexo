using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Agent.Models;
using Ashlar.Core.Application.Agent.Ports;

namespace Ashlar.Core.Application.Agent.UseCases.ListAgents;

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

    /// <summary>Creates a handler that lists agents from <see cref="IAgentRegistry"/>.</summary>
    /// <param name="agentRegistry">Registry of available agents.</param>
    /// <param name="logger">Logger for discovery operations.</param>
    public ListAgentsHandler(
        IAgentRegistry agentRegistry,
        ILogger<ListAgentsHandler> logger)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Handles the query by returning registered agent metadata.</summary>
    /// <param name="request">List-agents query (no parameters).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Metadata for all registered agents.</returns>
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

