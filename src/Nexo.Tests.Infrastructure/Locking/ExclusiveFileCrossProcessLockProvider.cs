using System.Security.Cryptography;
using System.Text;

namespace Nexo.Tests.Infrastructure.Locking;

/// <summary>
/// Cross-process lock via an exclusively opened file.
/// On Linux, another process holding <see cref="FileShare.None"/> typically causes <see cref="IOException"/> rather than blocking,
/// so acquisition polls until success or timeout.
/// </summary>
/// <remarks>
/// Subclass and override <see cref="ResolveLockPath"/> or <see cref="SanitizeLockName"/> to customize naming without replacing the whole provider.
/// </remarks>
public class ExclusiveFileCrossProcessLockProvider : ICrossProcessLockProvider
{
    /// <summary>Creates a provider with optional defaults applied to every <see cref="AcquireAsync"/> call.</summary>
    public ExclusiveFileCrossProcessLockProvider(CrossProcessLockOptions? defaultOptions = null)
    {
        DefaultOptions = CrossProcessLockDefaults.DefaultOptions.Merge(defaultOptions ?? new CrossProcessLockOptions());
    }

    /// <summary>Baseline options merged with per-call overrides.</summary>
    protected CrossProcessLockOptions DefaultOptions { get; }

    /// <inheritdoc />
    public async Task<ICrossProcessLock> AcquireAsync(
        string name,
        CrossProcessLockOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var merged = DefaultOptions.Merge(options ?? new CrossProcessLockOptions());
        var maxWait = merged.MaxWait ?? TimeSpan.FromMinutes(3);
        var poll = merged.PollInterval ?? TimeSpan.FromMilliseconds(150);
        var deadline = DateTimeOffset.UtcNow + maxWait;
        var path = ResolveLockPath(name, merged);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var stream = new FileStream(
                    path,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.Asynchronous | FileOptions.DeleteOnClose);

                return new ExclusiveFileCrossProcessLock(stream, path);
            }
            catch (IOException)
            {
                await Task.Delay(poll, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TimeoutException($"Could not acquire exclusive lock for '{path}' within {maxWait}.");
    }

    /// <summary>
    /// Builds the lock file path. Override to change layout (per-repo subdirectory, hashing only, etc.).
    /// </summary>
    protected virtual string ResolveLockPath(string lockName, CrossProcessLockOptions merged)
    {
        var dir = merged.DirectoryPath ?? Path.GetTempPath();
        var prefix = merged.FileNamePrefix ?? "nexo-cross-process";
        var suffix = merged.FileNameSuffix ?? ".lock";
        var safe = SanitizeLockName(lockName);
        return Path.Combine(dir, $"{prefix}.{safe}{suffix}");
    }

    /// <summary>
    /// Produces a filesystem-safe segment from <paramref name="lockName"/>. Override for custom rules.
    /// </summary>
    protected virtual string SanitizeLockName(string lockName)
    {
        var trimmed = lockName.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
            sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);

        var s = sb.ToString();
        if (s.Length <= 120 && s.Length > 0)
            return s;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lockName)))[..24];
        return hash;
    }
}
