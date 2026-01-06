using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Communication.Models;

namespace Nexo.Orchestration.Communication;

/// <summary>
/// Manages direct communication channels between specific agents.
/// 
/// Responsibilities:
/// - Creates bidirectional channels between agent pairs
/// - Validates message routing through channels
/// - Integrates with IAgentBus for message delivery
/// - Manages channel lifecycle
/// 
/// Used for direct agent-to-agent communication scenarios.
/// Provides a higher-level abstraction over the message bus.
/// </summary>
public sealed class ChannelManager
{
    private readonly IAgentBus _bus;
    private readonly ILogger<ChannelManager> _logger;
    private readonly Dictionary<string, Channel> _channels = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelManager"/> class.
    /// </summary>
    /// <param name="bus">The message bus for publishing messages.</param>
    /// <param name="logger">The logger instance.</param>
    public ChannelManager(IAgentBus bus, ILogger<ChannelManager> logger)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a bidirectional channel between two agents.
    /// 
    /// Channel IDs are normalized (sorted alphabetically) to ensure consistency.
    /// If a channel already exists between the two agents, returns the existing channel ID.
    /// </summary>
    /// <param name="agentId1">The ID of the first agent.</param>
    /// <param name="agentId2">The ID of the second agent.</param>
    /// <returns>The channel ID (normalized, format: "agent1-agent2").</returns>
    public string CreateChannel(string agentId1, string agentId2)
    {
        var channelId = $"{agentId1}-{agentId2}";
        if (string.Compare(agentId1, agentId2, StringComparison.OrdinalIgnoreCase) > 0)
        {
            channelId = $"{agentId2}-{agentId1}";
        }

        if (!_channels.ContainsKey(channelId))
        {
            _channels[channelId] = new Channel
            {
                ChannelId = channelId,
                AgentId1 = agentId1,
                AgentId2 = agentId2
            };
            _logger.LogDebug("Created channel {ChannelId} between {Agent1} and {Agent2}",
                channelId, agentId1, agentId2);
        }

        return channelId;
    }

    /// <summary>
    /// Sends a message through a channel.
    /// 
    /// Validates that the message is from one of the channel participants,
    /// then automatically routes it to the other participant via the message bus.
    /// </summary>
    /// <param name="channelId">The channel ID to send the message through.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if channel doesn't exist or message is from an invalid sender.</exception>
    public async Task SendAsync(string channelId, AgentMessage message, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            throw new InvalidOperationException($"Channel {channelId} does not exist");
        }

        // Validate that the message is from one of the channel participants
        if (message.FromAgentId != channel.AgentId1 && message.FromAgentId != channel.AgentId2)
        {
            throw new InvalidOperationException(
                $"Message from {message.FromAgentId} is not allowed on channel {channelId}");
        }

        // Set the target agent ID
        var targetAgentId = message.FromAgentId == channel.AgentId1
            ? channel.AgentId2
            : channel.AgentId1;

        var channelMessage = message with { ToAgentId = targetAgentId };
        await _bus.PublishAsync(channelMessage, cancellationToken);
    }

    /// <summary>
    /// Gets all channel IDs for a specific agent.
    /// 
    /// Returns all channels where the agent is a participant (either agent1 or agent2).
    /// </summary>
    /// <param name="agentId">The ID of the agent to get channels for.</param>
    /// <returns>A read-only list of channel IDs.</returns>
    public IReadOnlyList<string> GetChannelsForAgent(string agentId)
    {
        return _channels.Values
            .Where(c => c.AgentId1 == agentId || c.AgentId2 == agentId)
            .Select(c => c.ChannelId)
            .ToList();
    }

    private sealed class Channel
    {
        public required string ChannelId { get; init; }
        public required string AgentId1 { get; init; }
        public required string AgentId2 { get; init; }
    }
}

