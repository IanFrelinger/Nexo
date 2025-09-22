using System.Threading;
using System.Threading.Tasks;
using Nexo.Agent.Models;

namespace Nexo.Agent.Contracts;

/// <summary>
/// Core agent interface that plans tasks and executes them using available tools.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Executes a task by planning and executing the necessary steps.
    /// </summary>
    /// <param name="goal">The user's goal or task description</param>
    /// <param name="context">Additional context for the task</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the task execution</returns>
    Task<AgentResult> ExecuteTaskAsync(string goal, string? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the agent.
    /// </summary>
    AgentStatus Status { get; }

    /// <summary>
    /// Gets the current mode of operation (OFF/HYBRID/EMBEDDED).
    /// </summary>
    AgentMode Mode { get; }

    /// <summary>
    /// Sets the agent mode.
    /// </summary>
    /// <param name="mode">The new mode</param>
    void SetMode(AgentMode mode);
}

/// <summary>
/// Result of an agent task execution.
/// </summary>
/// <param name="Success">Whether the task completed successfully</param>
/// <param name="Message">Result message or error description</param>
/// <param name="Outputs">Generated outputs from the task</param>
/// <param name="Plan">The plan that was executed</param>
/// <param name="Duration">Total execution duration</param>
public record AgentResult(
    bool Success,
    string Message,
    IReadOnlyList<string> Outputs,
    Plan? Plan,
    TimeSpan Duration);

/// <summary>
/// Current status of the agent.
/// </summary>
public enum AgentStatus
{
    Idle,
    Planning,
    Executing,
    WaitingForTool,
    Error
}

/// <summary>
/// Agent operation mode.
/// </summary>
public enum AgentMode
{
    Off,
    Hybrid,
    Embedded
}
