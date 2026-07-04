namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Artifact produced by a capability (source, binary, config).
/// </summary>
public record Artifact
{
    /// <summary>Format of the artifact payload.</summary>
    public required ArtifactFormat Format { get; init; }

    /// <summary>Raw artifact bytes.</summary>
    public required byte[] Content { get; init; }

    /// <summary>Optional MIME-like content type hint.</summary>
    public string? ContentType { get; init; }
}
