using Ashlar.Core.Application.Execution.Ports;

namespace Ashlar.Infrastructure.Certification.HotSwap;

/// <summary>
/// Streams file content into a sandbox session over <see cref="ISandboxedSession.ExecAsync"/>
/// as base64 argv chunks — the no-mounts transfer path shared by the in-session build and
/// execution legs (sessions may be sibling containers on a remote daemon, where host bind
/// mounts are meaningless).
/// </summary>
internal static class SessionFileTransfer
{
    /// <summary>
    /// Raw bytes per chunk. Base64 expands 4/3, so 64 KiB raw is ~87 KiB of argv —
    /// comfortably under Linux's 128 KiB single-argument ceiling (MAX_ARG_STRLEN).
    /// </summary>
    internal const int ChunkSize = 64 * 1024;

    /// <summary>
    /// Uploads <paramref name="bytes"/> to <paramref name="path"/> inside the session.
    /// Returns null on success, or the failing step's result. Paths must be caller-owned
    /// constants; the chunk rides as its own argv element ($1), never inside script text —
    /// no quoting surface at all.
    /// </summary>
    public static async Task<ProcessCommandResult?> UploadAsync(
        ISandboxedSession session, string path, byte[] bytes, CancellationToken cancellationToken)
    {
        var b64Path = path + ".b64";
        for (var offset = 0; offset < bytes.Length; offset += ChunkSize)
        {
            var chunk = Convert.ToBase64String(bytes, offset, Math.Min(ChunkSize, bytes.Length - offset));
            var append = await session.ExecAsync(
                new[] { "sh", "-c", $"printf '%s' \"$1\" >> {b64Path}", "ashlar", chunk }, cancellationToken)
                .ConfigureAwait(false);
            if (!append.Succeeded)
                return append;
        }

        if (bytes.Length == 0)
        {
            var touch = await session.ExecAsync(
                new[] { "sh", "-c", $": > {path}" }, cancellationToken).ConfigureAwait(false);
            return touch.Succeeded ? null : touch;
        }

        var decode = await session.ExecAsync(
            new[] { "sh", "-c", $"base64 -d {b64Path} > {path} && rm {b64Path}" }, cancellationToken)
            .ConfigureAwait(false);
        return decode.Succeeded ? null : decode;
    }
}
