namespace Nexo.Commercial.GameDomain.Discord;
/// <summary>
/// Connects to Discord Gateway via websocket to listen for reaction events.
/// Used for the approval/denial flow: bot posts fix proposals, user reacts,
/// client receives the reaction and dispatches the appropriate action.
/// 
/// Rate limit: reactions are limited to 1 per 250ms on the gateway.
/// Heartbeat interval is provided by Discord on HELLO (typically 41250ms).
/// </summary>
public sealed class DiscordGatewayClient : IDisposable
{
    private readonly string _botToken;
    private readonly string _channelId;
    private readonly Dictionary<string, PendingApproval> _pendingApprovals = new();
    private readonly object _lock = new();

    public DiscordGatewayClient(string botToken, string channelId)
    {
        _botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
        _channelId = channelId ?? throw new ArgumentNullException(nameof(channelId));
    }

    /// <summary>
    /// Register a message as awaiting approval. Maps message ID to a callback.
    /// When the expected reaction emoji is received, the callback fires.
    /// </summary>
    public void RegisterApproval(string messageId, Func<string, Task> onReaction)
    {
        lock (_lock)
        {
            _pendingApprovals[messageId] = new PendingApproval
            {
                MessageId = messageId,
                OnReaction = onReaction,
                RegisteredAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Process an incoming reaction event. Called by the gateway event loop.
    /// Returns true if the reaction was handled (matched a pending approval).
    /// </summary>
    public async Task<bool> HandleReactionAsync(string messageId, string emoji, string userId)
    {
        PendingApproval? approval;
        lock (_lock)
        {
            if (!_pendingApprovals.TryGetValue(messageId, out approval))
                return false;
        }

        await approval.OnReaction(emoji);

        lock (_lock)
        {
            _pendingApprovals.Remove(messageId);
        }

        return true;
    }

    /// <summary>
    /// Clean up approvals older than the specified age.
    /// Prevents memory leak from unhandled proposals.
    /// </summary>
    public void CleanupStaleApprovals(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        lock (_lock)
        {
            var stale = _pendingApprovals
                .Where(kv => kv.Value.RegisteredAt < cutoff)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in stale)
                _pendingApprovals.Remove(key);
        }
    }

    public int PendingCount
    {
        get { lock (_lock) { return _pendingApprovals.Count; } }
    }

    /// <summary>Releases resources held by this instance.</summary>
    public void Dispose()
    {
        lock (_lock) { _pendingApprovals.Clear(); }
    }

    private sealed class PendingApproval
    {
        /// <summary>message id value.</summary>
        public string MessageId { get; init; } = string.Empty;
        /// <summary>On reaction value.</summary>
        public Func<string, Task> OnReaction { get; init; } = _ => Task.CompletedTask;
        /// <summary>Registered at value.</summary>
        public DateTimeOffset RegisteredAt { get; init; }
    }
}
