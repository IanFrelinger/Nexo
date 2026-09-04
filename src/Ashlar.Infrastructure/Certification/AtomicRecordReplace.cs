namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Moves a private staging file over a live certification record, retrying the Windows
/// sharing collision that makes a concurrent overwrite throw instead of replace.
/// </summary>
/// <remarks>
/// Unix <c>rename</c> replaces unconditionally. Windows <c>MoveFileEx(...,
/// MOVEFILE_REPLACE_EXISTING)</c> fails with access-denied when another writer (or a
/// reader that opened the destination) still holds the file. The staged bytes are one
/// call's private file, so a failed move leaves them in place for the next attempt; the
/// live record is the last successful replace, never a mix of two writers.
/// </remarks>
internal static class AtomicRecordReplace
{
    internal const int MaxAttempts = 32;

    /// <summary>
    /// Moves <paramref name="staged"/> over <paramref name="destination"/>.
    /// </summary>
    /// <param name="staged">This call's private staging file.</param>
    /// <param name="destination">The live record path.</param>
    /// <param name="move">
    /// Overlay used by tests to inject the Windows sharing exception. Production
    /// callers omit it and get <see cref="File.Move(string, string, bool)"/>.
    /// </param>
    internal static void IntoPlace(
        string staged,
        string destination,
        Action<string, string>? move = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staged);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        move ??= static (source, dest) => File.Move(source, dest, overwrite: true);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                move(staged, destination);
                return;
            }
            catch (Exception ex) when (
                attempt < MaxAttempts &&
                File.Exists(staged) &&
                (ex is IOException || ex is UnauthorizedAccessException))
            {
                if (attempt < 8)
                    Thread.SpinWait(64 * attempt);
                else
                    Thread.Sleep(TimeSpan.FromMilliseconds(Math.Min(50, 2 * attempt)));
            }
        }
    }
}
