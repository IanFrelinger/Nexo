namespace Ashlar.Tests.Infrastructure.Locking;

/// <summary>
/// Acquires locks that work across processes (e.g. parallel <c>dotnet test</c> TFMs).
/// Implementations may use OS-specific primitives; callers should treat behavior as best-effort mutual exclusion.
/// </summary>
public interface ICrossProcessLockProvider
{
    /// <summary>
    /// Blocks until the lock is acquired or <paramref name="cancellationToken"/> / options limits are reached.
    /// </summary>
    /// <param name="name">Logical lock name (sanitized to a file segment).</param>
    /// <param name="options">Optional overrides; merged with provider defaults.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ICrossProcessLock> AcquireAsync(
        string name,
        CrossProcessLockOptions? options = null,
        CancellationToken cancellationToken = default);
}
