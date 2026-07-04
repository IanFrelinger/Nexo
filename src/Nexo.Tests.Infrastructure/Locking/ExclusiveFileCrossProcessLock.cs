namespace Nexo.Tests.Infrastructure.Locking;

/// <summary>Exclusive file cross process lock.</summary>
internal sealed class ExclusiveFileCrossProcessLock : ICrossProcessLock
{
    private readonly FileStream _stream;

    public ExclusiveFileCrossProcessLock(FileStream stream, string lockPath)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        LockPath = lockPath ?? throw new ArgumentNullException(nameof(lockPath));
    }

    /// <summary>Lock path.</summary>
    public string LockPath { get; }

    /// <summary>Dispose async.</summary>
    public ValueTask DisposeAsync() => _stream.DisposeAsync();
}
