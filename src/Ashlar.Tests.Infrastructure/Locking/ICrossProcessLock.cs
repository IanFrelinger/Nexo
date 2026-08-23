namespace Ashlar.Tests.Infrastructure.Locking;

/// <summary>
/// An acquired cross-process lock. Dispose when the critical section ends (typically <c>await using</c>).
/// </summary>
public interface ICrossProcessLock : IAsyncDisposable
{
    /// <summary>Resolved filesystem path backing this lock (useful for diagnostics).</summary>
    string LockPath { get; }
}
