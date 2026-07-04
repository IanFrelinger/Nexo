using Nexo.Core.Application.Common.Ports;

namespace Nexo.Infrastructure.IO;

/// <summary>
/// Infrastructure implementation of text file IO.
/// Keeps System.IO usage out of Core.Application.
/// </summary>
public sealed class LocalTextFileSystem : ITextFileSystem
{
    /// <summary>Read all text asynchronously.</summary>
    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
        => File.ReadAllTextAsync(path, ct);

    /// <summary>Write all text asynchronously.</summary>
    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, ct);

    /// <summary>Write all bytes asynchronously.</summary>
    public Task WriteAllBytesAsync(string path, byte[] content, CancellationToken ct = default)
        => File.WriteAllBytesAsync(path, content, ct);
}

