using System;

namespace Nexo.Core.Domain.Evidence;

/// <summary>One file captured under an evidence bundle root.</summary>
public sealed class EvidenceArtifact
{
    /// <summary>Creates an evidence artifact descriptor.</summary>
    public EvidenceArtifact(
        EvidenceKind kind,
        string relativePath,
        long bytes,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path is required.", nameof(relativePath));
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes));

        Kind = kind;
        RelativePath = relativePath.Replace('\\', '/');
        Bytes = bytes;
        CreatedAt = createdAt;
    }

    /// <summary>Artifact kind.</summary>
    public EvidenceKind Kind { get; }

    /// <summary>Path relative to the evidence root (forward slashes).</summary>
    public string RelativePath { get; }

    /// <summary>File size in bytes.</summary>
    public long Bytes { get; }

    /// <summary>When the artifact was recorded.</summary>
    public DateTimeOffset CreatedAt { get; }
}
