using MediatR;
using Nexo.Core.Application.Agent.Models;

namespace Nexo.Core.Application.Agent.UseCases.ListAgents;

/// <summary>
/// Query for listing all available agents.
/// </summary>
public record ListAgentsQuery : IRequest<IReadOnlyList<AgentMetadata>>;

