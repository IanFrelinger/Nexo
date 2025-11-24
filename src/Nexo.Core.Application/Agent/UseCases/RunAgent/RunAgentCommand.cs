using MediatR;
using Nexo.Core.Application.Agent.Models;

namespace Nexo.Core.Application.Agent.UseCases.RunAgent;

/// <summary>
/// Command to run an agent action.
/// </summary>
public record RunAgentCommand(string AgentName, FileInfo? InputFile) : IRequest<AgentExecutionResult>;

