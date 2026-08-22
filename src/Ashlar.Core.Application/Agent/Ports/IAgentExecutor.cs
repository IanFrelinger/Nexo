using Ashlar.Core.Application.Agent.Models;

namespace Ashlar.Core.Application.Agent.Ports;

/// <summary>
/// Port for executing agent actions.
/// 
/// Defines the contract for executing agents:
/// - Execute an agent by name with optional input file
/// - Returns AgentExecutionResult with outcome
/// 
/// Implementations (AgentExecutorAdapter) provide agent execution logic.
/// Used by CLI commands to run specific agents.
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

