using MediatR;
using Ashlar.Core.Application.Agent.Models;

namespace Ashlar.Core.Application.Agent.UseCases.ListAgents;

/// <summary>
/// Query for listing all available agents.
/// </summary>
public record ListAgentsQuery : IRequest<IReadOnlyList<AgentMetadata>>;

