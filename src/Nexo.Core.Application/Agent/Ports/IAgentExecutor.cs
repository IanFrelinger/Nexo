using Nexo.Core.Application.Agent.Models;

namespace Nexo.Core.Application.Agent.Ports;

/// <summary>
/// Port for executing agent actions.
/// </summary>
public interface IAgentExecutor
{
    /// <summary>
    /// Executes an agent by name with optional input file.
    /// </summary>
    Task<AgentExecutionResult> ExecuteAsync(
        string agentName,
        FileInfo? inputFile,
        CancellationToken cancellationToken = default);
}

