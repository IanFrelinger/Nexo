using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Communication.Models;

namespace Nexo.Orchestration.Communication;

/// <summary>
/// In-memory pub/sub message bus for agent communication.
/// </summary>
public sealed class AgentBus : IAgentBus
{
    private readonly ILogger<AgentBus> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<MessageSubscription>> _subscriptions = new();
    private readonly ConcurrentQueue<AgentMessage> _messageHistory = new();
    private readonly object _lock = new();

    public AgentBus(ILogger<AgentBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task PublishAsync(AgentMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        _logger.LogDebug("Publishing message {MessageType} from {FromAgentId} to {ToAgentId}",
            message.MessageType, message.FromAgentId, message.ToAgentId ?? "all");

        // Store in history (limit to last 1000 messages)
        _messageHistory.Enqueue(message);
        while (_messageHistory.Count > 1000)
        {
            _messageHistory.TryDequeue(out _);
        }

        // Notify subscribers
        var subscriptions = GetMatchingSubscriptions(message);
        foreach (var subscription in subscriptions)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await subscription.Handler(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling message {MessageId} in subscription", message.MessageId);
                }
            }, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task<IDisposable> SubscribeAsync(
        string? messageType,
        Func<AgentMessage, CancellationToken, Task> handler,
        string? agentId = null,
        CancellationToken cancellationToken = default)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var key = messageType ?? "*";
        var subscription = new MessageSubscription
        {
            MessageType = messageType,
            AgentId = agentId,
            Handler = handler,
            Id = Guid.NewGuid().ToString()
        };

        _subscriptions.AddOrUpdate(key, new ConcurrentBag<MessageSubscription> { subscription },
            (k, existing) =>
            {
                existing.Add(subscription);
                return existing;
            });

        _logger.LogDebug("Subscribed to {MessageType} for agent {AgentId}", messageType ?? "all", agentId ?? "all");

        return Task.FromResult<IDisposable>(new SubscriptionToken(key, subscription.Id, this));
    }

    public Task<IReadOnlyList<AgentMessage>> GetMessagesForAgentAsync(
        string agentId,
        string? messageType = null,
        CancellationToken cancellationToken = default)
    {
        var messages = _messageHistory
            .Where(m =>
                (m.ToAgentId == null || m.ToAgentId == agentId || m.FromAgentId == agentId) &&
                (messageType == null || m.MessageType == messageType))
            .ToList();

        return Task.FromResult<IReadOnlyList<AgentMessage>>(messages);
    }

    private IEnumerable<MessageSubscription> GetMatchingSubscriptions(AgentMessage message)
    {
        var subscriptions = new List<MessageSubscription>();

        // Get subscriptions for specific message type
        if (_subscriptions.TryGetValue(message.MessageType, out var typeSubscriptions))
        {
            subscriptions.AddRange(typeSubscriptions);
        }

        // Get subscriptions for all messages
        if (_subscriptions.TryGetValue("*", out var allSubscriptions))
        {
            subscriptions.AddRange(allSubscriptions);
        }

        // Filter by agent ID if specified
        if (!string.IsNullOrEmpty(message.ToAgentId))
        {
            subscriptions = subscriptions
                .Where(s => s.AgentId == null || s.AgentId == message.ToAgentId)
                .ToList();
        }

        return subscriptions;
    }

    internal void Unsubscribe(string key, string subscriptionId)
    {
        if (_subscriptions.TryGetValue(key, out var subscriptions))
        {
            var subscription = subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription != null)
            {
                var newSubscriptions = new ConcurrentBag<MessageSubscription>(
                    subscriptions.Where(s => s.Id != subscriptionId));
                _subscriptions.TryUpdate(key, newSubscriptions, subscriptions);
                _logger.LogDebug("Unsubscribed {SubscriptionId} from {MessageType}", subscriptionId, key);
            }
        }
    }

    private sealed class MessageSubscription
    {
        public required string Id { get; init; }
        public string? MessageType { get; init; }
        public string? AgentId { get; init; }
        public required Func<AgentMessage, CancellationToken, Task> Handler { get; init; }
    }

    private sealed class SubscriptionToken : IDisposable
    {
        private readonly string _key;
        private readonly string _subscriptionId;
        private readonly AgentBus _bus;
        private bool _disposed;

        public SubscriptionToken(string key, string subscriptionId, AgentBus bus)
        {
            _key = key;
            _subscriptionId = subscriptionId;
            _bus = bus;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _bus.Unsubscribe(_key, _subscriptionId);
                _disposed = true;
            }
        }
    }
}

