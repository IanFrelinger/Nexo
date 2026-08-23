using System.Text.Json;

namespace Ashlar.Orchestration.Communication.Models;

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
