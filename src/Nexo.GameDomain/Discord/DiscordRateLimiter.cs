namespace Nexo.GameDomain.Discord;

/// <summary>
/// Token-bucket rate limiter for Discord API calls.
/// Stub: will be replaced by Phase 2 implementation.
/// </summary>
public sealed class DiscordRateLimiter
{
    private readonly int _maxPerSecond;

    public DiscordRateLimiter(int maxPerSecond = 5)
    {
        _maxPerSecond = maxPerSecond;
    }

    public Task WaitAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async Task ExecuteAsync(Func<Task> action)
    {
        await WaitAsync();
        await action();
    }
}
