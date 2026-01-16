using Nexo.Core.Application.Common.Ports;

namespace Nexo.Infrastructure.IO;

/// <summary>
/// Infrastructure implementation of text file IO.
/// Keeps System.IO usage out of Core.Application.
/// </summary>
public sealed class LocalTextFileSystem : ITextFileSystem
{
    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
        => File.ReadAllTextAsync(path, ct);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, ct);
}

