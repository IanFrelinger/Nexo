using System;
using System.IO;

namespace Nexo.Core.Domain.HostBridge;

/// <summary>
/// Sandboxed root directory for playtest / host evidence artifacts.
/// Relative paths must resolve under this root.
/// </summary>
public sealed class ArtifactRootPath
{
    /// <summary>Creates an artifact root, ensuring the directory exists.</summary>
    public ArtifactRootPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Artifact root path is required.", nameof(path));

        FullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(FullPath);
    }

    /// <summary>Absolute root path with a trailing directory separator.</summary>
    public string FullPath { get; }

    /// <summary>
    /// Resolves <paramref name="relativePath"/> under this root.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the path escapes the root.</exception>
    public string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path is required.", nameof(relativePath));

        var full = Path.GetFullPath(Path.Combine(FullPath, relativePath));
        if (!full.StartsWith(FullPath, StringComparison.Ordinal))
            throw new InvalidOperationException("Artifact path escaped the configured root.");

        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return full;
    }
}
