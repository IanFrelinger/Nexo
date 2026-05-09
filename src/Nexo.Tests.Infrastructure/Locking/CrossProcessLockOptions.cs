namespace Nexo.Tests.Infrastructure.Locking;

/// <summary>
/// Configuration for <see cref="ICrossProcessLockProvider"/> / exclusive-file locks.
/// Immutable overrides merge with provider defaults (last write wins per property).
/// </summary>
public sealed record CrossProcessLockOptions
{
    /// <summary>Maximum wall-clock time to wait when another process holds the lock.</summary>
    public TimeSpan? MaxWait { get; init; }

    /// <summary>Delay between attempts when the lock file is busy (Linux often throws instead of blocking).</summary>
    public TimeSpan? PollInterval { get; init; }

    /// <summary>Directory for lock files; default is <see cref="Path.GetTempPath"/>.</summary>
    public string? DirectoryPath { get; init; }

    /// <summary>Prefix for lock filenames.</summary>
    public string? FileNamePrefix { get; init; }

    /// <summary>Suffix including extension (e.g. <c>.integration.lock</c>).</summary>
    public string? FileNameSuffix { get; init; }

    /// <summary>Merges <paramref name="overrides"/> onto this instance (override wins when set).</summary>
    public CrossProcessLockOptions Merge(CrossProcessLockOptions? overrides)
    {
        if (overrides is null)
            return this;

        return this with
        {
            MaxWait = overrides.MaxWait ?? MaxWait,
            PollInterval = overrides.PollInterval ?? PollInterval,
            DirectoryPath = overrides.DirectoryPath ?? DirectoryPath,
            FileNamePrefix = overrides.FileNamePrefix ?? FileNamePrefix,
            FileNameSuffix = overrides.FileNameSuffix ?? FileNameSuffix,
        };
    }
}
