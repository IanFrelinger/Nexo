using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;
using Nexo.Orchestration.Communication.Models;

namespace Nexo.Orchestration.Communication;

/// <summary>
/// Bridges the local IAgentBus with INetworkBus: forwards agent messages to the network
/// and delivers incoming network agent-message events to the local bus.
/// </summary>
public sealed class AgentBusNetworkBridge : IHostedService, IDisposable
{
    private readonly IAgentBus _agentBus;
    private readonly INetworkBus _networkBus;
    private readonly AgentBusNetworkBridgeOptions _options;
    private readonly ILogger<AgentBusNetworkBridge> _logger;
    private IDisposable? _networkSubscription;
    private IDisposable? _agentSubscription;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AgentBusNetworkBridge(
        IAgentBus agentBus,
        INetworkBus networkBus,
        IOptions<AgentBusNetworkBridgeOptions> options,
        ILogger<AgentBusNetworkBridge> logger)
    {
        _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
        _networkBus = networkBus ?? throw new ArgumentNullException(nameof(networkBus));
        _options = options?.Value ?? new AgentBusNetworkBridgeOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _networkSubscription = await _networkBus.SubscribeAsync(NetworkEventTypes.AgentMessage, OnNetworkEventAsync, cancellationToken)
            .ConfigureAwait(false);
        _agentSubscription = await _agentBus.SubscribeAsync(null, OnAgentMessageAsync, null, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation("AgentBusNetworkBridge started: forwarding agent messages to network and delivering network agent messages locally.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _networkSubscription?.Dispose();
        _networkSubscription = null;
        _agentSubscription?.Dispose();
        _agentSubscription = null;
    }

    private async Task OnNetworkEventAsync(NetworkEvent evt, CancellationToken cancellationToken)
    {
        if (!evt.Payload.HasValue || evt.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            _logger.LogDebug("Ignoring network agent event {EventId}: missing or invalid payload", evt.EventId);
            return;
        }

        AgentMessageWireDto? dto;
        try
        {
            dto = evt.Payload.Value.Deserialize<AgentMessageWireDto>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize agent message from event {EventId}", evt.EventId);
            return;
        }

        if (dto?.MessageId == null || dto.MessageType == null)
        {
            _logger.LogDebug("Ignoring network agent event {EventId}: invalid message shape", evt.EventId);
            return;
        }

        var message = new ForwardedAgentMessage
        {
            MessageId = dto.MessageId,
            FromAgentId = dto.FromAgentId ?? "",
            ToAgentId = dto.ToAgentId,
            MessageType = dto.MessageType,
            Timestamp = dto.Timestamp,
            Payload = dto.Payload,
            SourceNodeId = evt.SourceNodeId
        };

        await _agentBus.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Delivered network agent message {MessageId} ({MessageType}) from node {SourceNodeId} to local bus",
            message.MessageId, message.MessageType, evt.SourceNodeId);
    }

    private async Task OnAgentMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        if (message is ForwardedAgentMessage)
            return;
        if (!_options.ForwardAgentMessagesToNetwork)
            return;
        if (_options.ForwardMessageTypes != null && _options.ForwardMessageTypes.Count > 0
            && !_options.ForwardMessageTypes.Contains(message.MessageType, StringComparer.OrdinalIgnoreCase))
            return;

        var payload = JsonSerializer.SerializeToElement(new AgentMessageWireDto
        {
            MessageId = message.MessageId,
            FromAgentId = message.FromAgentId,
            ToAgentId = message.ToAgentId,
            MessageType = message.MessageType,
            Timestamp = message.Timestamp,
            Payload = message.Payload
        }, JsonOptions);

        var evt = new NetworkEvent
        {
            EventId = message.MessageId,
            SourceNodeId = _networkBus.NodeId,
            TargetNodeId = null,
            EventType = NetworkEventTypes.AgentMessage,
            Timestamp = DateTimeOffset.UtcNow,
            Hops = 0,
            MaxHops = _options.MaxHops,
            Payload = payload
        };

        await _networkBus.BroadcastAsync(evt, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Forwarded agent message {MessageId} ({MessageType}) to network", message.MessageId, message.MessageType);
    }

    private sealed class AgentMessageWireDto
    {
        public string? MessageId { get; set; }
        public string? FromAgentId { get; set; }
        public string? ToAgentId { get; set; }
        public string? MessageType { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public JsonElement? Payload { get; set; }
    }
}
