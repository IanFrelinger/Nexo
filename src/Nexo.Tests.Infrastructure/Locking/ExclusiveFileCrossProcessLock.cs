namespace Nexo.Tests.Infrastructure.Locking;

internal sealed class ExclusiveFileCrossProcessLock : ICrossProcessLock
{
    private readonly FileStream _stream;

    public ExclusiveFileCrossProcessLock(FileStream stream, string lockPath)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        LockPath = lockPath ?? throw new ArgumentNullException(nameof(lockPath));
    }

    public string LockPath { get; }

    public ValueTask DisposeAsync() => _stream.DisposeAsync();
}
