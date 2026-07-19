using System;

namespace Nexo.Core.Application.Evidence.Ports;

/// <summary>Provider-agnostic upload options for <see cref="IArtifactSink"/>.</summary>
public sealed class ArtifactUploadRequest
{
    /// <summary>Creates an upload request.</summary>
    public ArtifactUploadRequest(
        string? targetFolderId = null,
        string? contentType = null,
        string? displayName = null,
        bool shareAnyoneWithLink = false)
    {
        TargetFolderId = targetFolderId;
        ContentType = contentType;
        DisplayName = displayName;
        ShareAnyoneWithLink = shareAnyoneWithLink;
    }

    /// <summary>Optional folder / container id at the sink.</summary>
    public string? TargetFolderId { get; }

    /// <summary>Optional MIME type.</summary>
    public string? ContentType { get; }

    /// <summary>Optional remote display name.</summary>
    public string? DisplayName { get; }

    /// <summary>When true, sinks may grant public read links.</summary>
    public bool ShareAnyoneWithLink { get; }
}
