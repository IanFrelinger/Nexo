using System.Text.Json;

namespace Nexo.Orchestration.Communication.Models;

/// <summary>
/// Base class for all agent messages.
/// 
/// Contains:
/// - Message identification (ID, type, timestamp)
/// - Sender and receiver agent IDs
/// - Message payload (JSON)
/// 
/// Derived message types:
/// - OutputEmitted: Agent produced output
/// - DependencyResolved: Dependency became available
/// - AgentStateChanged: Agent state transition
/// - AgentError: Agent encountered an error
/// - DataRequest: Request for data from another agent
/// - DataResponse: Response to a DataRequest
/// 
/// Used by IAgentBus for inter-agent communication.
/// </summary>
public abstract record AgentMessage
{
    /// <summary>
    /// Unique message ID.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// ID of the agent that sent this message.
    /// </summary>
    public required string FromAgentId { get; init; }

    /// <summary>
    /// ID of the agent that should receive this message (null for broadcast).
    /// </summary>
    public string? ToAgentId { get; init; }

    /// <summary>
    /// Message type identifier.
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Message payload (JSON).
    /// </summary>
    public JsonElement? Payload { get; init; }
}

/// <summary>
/// Message emitted when an agent produces output.
/// 
/// Contains the output produced by the agent and its schema identifier.
/// Used to notify dependent agents that output is available.
/// </summary>
public sealed record OutputEmitted : AgentMessage
{
    public OutputEmitted()
    {
        MessageType = "OutputEmitted";
    }

    /// <summary>
    /// The output produced by the agent.
    /// </summary>
    public required object Output { get; init; }

    /// <summary>
    /// Output schema identifier.
    /// </summary>
    public string? OutputSchema { get; init; }
}

/// <summary>
/// Message emitted when a dependency is resolved.
/// 
/// Contains the dependency agent ID and its output.
/// Used to notify waiting agents that their dependencies are ready.
/// </summary>
public sealed record DependencyResolved : AgentMessage
{
    public DependencyResolved()
    {
        MessageType = "DependencyResolved";
    }

    /// <summary>
    /// ID of the agent that resolved the dependency.
    /// </summary>
    public required string DependencyAgentId { get; init; }

    /// <summary>
    /// The resolved dependency output.
    /// </summary>
    public required object DependencyOutput { get; init; }
}

/// <summary>
/// Message emitted when an agent state changes.
/// 
/// Contains the new and previous state of the agent.
/// Used to notify other agents about state transitions.
/// </summary>
public sealed record AgentStateChanged : AgentMessage
{
    public AgentStateChanged()
    {
        MessageType = "AgentStateChanged";
    }

    /// <summary>
    /// The new state of the agent.
    /// </summary>
    public required string NewState { get; init; }

    /// <summary>
    /// The previous state of the agent.
    /// </summary>
    public string? PreviousState { get; init; }
}

/// <summary>
/// Message emitted when an agent encounters an error.
/// 
/// Contains error message, type, and optional stack trace.
/// Used to notify other agents and the orchestrator about failures.
/// </summary>
public sealed record AgentError : AgentMessage
{
    public AgentError()
    {
        MessageType = "AgentError";
    }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Error type/category.
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Stack trace (if available).
    /// </summary>
    public string? StackTrace { get; init; }
}

/// <summary>
/// Message for requesting data from another agent.
/// 
/// Contains the type of data requested and optional filter criteria.
/// Used for direct agent-to-agent data requests.
/// </summary>
public sealed record DataRequest : AgentMessage
{
    public DataRequest()
    {
        MessageType = "DataRequest";
    }

    /// <summary>
    /// Type of data requested.
    /// </summary>
    public required string RequestedDataType { get; init; }

    /// <summary>
    /// Optional filter criteria.
    /// </summary>
    public JsonElement? Filter { get; init; }
}

/// <summary>
/// Response to a DataRequest.
/// 
/// Contains the original request message ID and the requested data.
/// Used to respond to DataRequest messages.
/// </summary>
public sealed record DataResponse : AgentMessage
{
    public DataResponse()
    {
        MessageType = "DataResponse";
    }

    /// <summary>
    /// ID of the original request message.
    /// </summary>
    public required string RequestMessageId { get; init; }

    /// <summary>
    /// The requested data.
    /// </summary>
    public required object Data { get; init; }
}

