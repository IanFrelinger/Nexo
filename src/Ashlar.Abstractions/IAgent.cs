using System.Text.Json;

namespace Ashlar.Abstractions;

/// <summary>
/// Core abstractions for the Ashlar agent framework.
///
/// Defines the fundamental interfaces and types:
/// - IAgent: Agents that can think and make decisions
/// - ITool: Tools that agents can invoke
/// - IPolicy: Policies that approve/reject tool calls
/// - IModel: LLM models for AI operations
/// - IToolbox: Tool registry and memory provider
/// - IAgentMemory: Agent event storage and retrieval
///
/// These abstractions form the foundation of the agent execution model.
/// </summary>

/// <summary>
/// Interface for agents that can observe the world and make decisions.
///
/// Agents observe the world through AgentObservation and decide on actions
/// (ToolCalls) using ThinkAsync. They have access to a toolbox and memory.
/// </summary>
public interface IAgent
{
    /// <summary>Human-readable agent name used for logging, memory scoping, and routing.</summary>
    string Name { get; }

    /// <summary>
    /// Observes the world and decides which tool calls to propose for execution.
    /// </summary>
    /// <param name="obs">Current world observation.</param>
    /// <param name="tools">Toolbox providing tool schemas and invocation.</param>
    /// <param name="mem">Agent-scoped event memory.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Proposed actions, or <see cref="AgentActions.None"/> when no action is needed.</returns>
    Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct);
}
