using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Evidence;

/// <summary>Package of checkpoints and artifacts from one agent/host cycle.</summary>
public sealed class EvidenceBundle
{
    /// <summary>Creates an evidence bundle.</summary>
    public EvidenceBundle(
        string sessionId,
        string rootPath,
        IReadOnlyList<EvidenceCheckpoint> checkpoints,
        IReadOnlyList<EvidenceArtifact> artifacts,
        string? verdict = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path is required.", nameof(rootPath));

        SessionId = sessionId.Trim();
        RootPath = rootPath;
        Checkpoints = checkpoints ?? Array.Empty<EvidenceCheckpoint>();
        Artifacts = artifacts ?? Array.Empty<EvidenceArtifact>();
        Verdict = verdict;
    }

    /// <summary>Session identifier.</summary>
    public string SessionId { get; }

    /// <summary>Absolute evidence root directory.</summary>
    public string RootPath { get; }

    /// <summary>Ordered checkpoints.</summary>
    public IReadOnlyList<EvidenceCheckpoint> Checkpoints { get; }

    /// <summary>Captured artifacts.</summary>
    public IReadOnlyList<EvidenceArtifact> Artifacts { get; }

    /// <summary>Optional overall verdict (<c>passed</c> / <c>failed</c>).</summary>
    public string? Verdict { get; }
}
