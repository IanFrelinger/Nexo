namespace Ashlar.Infrastructure.IO;

/// <summary>
/// Infrastructure-only wrappers around raw binary filesystem APIs.
/// Keeps System.IO.File usage out of higher layers (CLI/Agents/Orchestration).
/// </summary>
public static class BinaryFile
{
    /// <summary>Exists.</summary>
    public static bool Exists(string path) => File.Exists(path);

    /// <summary>Write all bytes asynchronously.</summary>
    public static Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken ct = default)
        => File.WriteAllBytesAsync(path, bytes, ct);

    /// <summary>Write all bytes asynchronously.</summary>
    public static Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken ct = default)
        => File.WriteAllBytesAsync(path, bytes.ToArray(), ct);
}

