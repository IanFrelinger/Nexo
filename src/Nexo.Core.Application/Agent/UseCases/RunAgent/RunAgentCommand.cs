using MediatR;
using Nexo.Core.Application.Agent.Models;

namespace Nexo.Core.Application.Agent.UseCases.RunAgent;

/// <summary>Command to run an agent action.</summary>
/// <param name="AgentName">Registered agent identifier to execute.</param>
/// <param name="InputFile">Optional input file passed to the agent.</param>
public record RunAgentCommand(string AgentName, FileInfo? InputFile) : IRequest<AgentExecutionResult>;
