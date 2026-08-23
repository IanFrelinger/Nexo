namespace Ashlar.Tests.Infrastructure.Locking;

/// <summary>
/// Entry points and shared defaults for cross-process locking in test hosts.
/// </summary>
public static class CrossProcessLockDefaults
{
    /// <summary>Default options used by <see cref="ExclusiveFileCrossProcessLockProvider"/> when none are supplied.</summary>
    public static CrossProcessLockOptions DefaultOptions { get; } = new()
    {
        MaxWait = TimeSpan.FromMinutes(3),
        PollInterval = TimeSpan.FromMilliseconds(150),
        FileNamePrefix = "ashlar-cross-process",
        FileNameSuffix = ".lock",
    };

    /// <summary>
    /// Shared provider instance suitable for most tests (parallel TFMs, CLI integration, etc.).
    /// For isolation or custom defaults, construct <see cref="ExclusiveFileCrossProcessLockProvider"/> explicitly.
    /// </summary>
    public static ICrossProcessLockProvider SharedProvider { get; } =
        new ExclusiveFileCrossProcessLockProvider();
}
