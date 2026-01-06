using Nexo.Orchestration.Communication.Models;

namespace Nexo.Orchestration.Communication;

/// <summary>
/// Interface for pub/sub messaging between agents.
/// 
/// Provides asynchronous communication between agents:
/// - Publish messages to the bus
/// - Subscribe to message types
/// - Filter messages by agent ID
/// - Retrieve message history
/// 
/// Enables loose coupling and event-driven communication patterns.
/// </summary>
public interface IAgentBus
{
    /// <summary>
    /// Publishes a message to the bus.
    /// </summary>
    Task PublishAsync(AgentMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <param name="messageType">Type of messages to subscribe to (null for all messages).</param>
    /// <param name="handler">Handler function to process messages.</param>
    /// <param name="agentId">Optional agent ID filter (only receive messages for this agent).</param>
    /// <returns>Subscription token that can be used to unsubscribe.</returns>
    Task<IDisposable> SubscribeAsync(
        string? messageType,
        Func<AgentMessage, CancellationToken, Task> handler,
        string? agentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a specific agent.
    /// </summary>
    Task<IReadOnlyList<AgentMessage>> GetMessagesForAgentAsync(
        string agentId,
        string? messageType = null,
        CancellationToken cancellationToken = default);
}

