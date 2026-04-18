namespace Nexo.GameDomain.Discord;

/// <summary>
/// Token-bucket rate limiter respecting Discord's API limits.
/// Per-channel: 5 messages per 5 seconds. Global: 50 requests/second.
/// File uploads: same as messages. Reactions: 1 per 250ms.
/// </summary>
public sealed class DiscordRateLimiter
{
    private readonly SemaphoreSlim _channelSemaphore = new(5);
    private readonly SemaphoreSlim _globalSemaphore = new(50);
    private readonly TimeSpan _channelWindow = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _globalWindow = TimeSpan.FromSeconds(1);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await _globalSemaphore.WaitAsync(ct);
        await _channelSemaphore.WaitAsync(ct);
        try
        {
            var result = await action();
            return result;
        }
        finally
        {
            _ = ReleaseAfterDelay(_channelSemaphore, _channelWindow);
            _ = ReleaseAfterDelay(_globalSemaphore, _globalWindow);
        }
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken ct = default)
    {
        await ExecuteAsync(async () => { await action(); return 0; }, ct);
    }

    private static async Task ReleaseAfterDelay(SemaphoreSlim semaphore, TimeSpan delay)
    {
        await Task.Delay(delay);
        semaphore.Release();
    }
}
