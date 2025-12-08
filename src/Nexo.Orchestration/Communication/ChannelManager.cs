using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Communication.Models;

namespace Nexo.Orchestration.Communication;

/// <summary>
/// Manages direct communication channels between specific agents.
/// </summary>
public sealed class ChannelManager
{
    private readonly IAgentBus _bus;
    private readonly ILogger<ChannelManager> _logger;
    private readonly Dictionary<string, Channel> _channels = new();

    public ChannelManager(IAgentBus bus, ILogger<ChannelManager> logger)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a channel between two agents.
    /// </summary>
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
    /// </summary>
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
    /// Gets all channels for an agent.
    /// </summary>
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

