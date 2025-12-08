using System.Text.Json;

namespace Nexo.Orchestration.Communication.Models;

/// <summary>
/// Base class for all agent messages.
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

