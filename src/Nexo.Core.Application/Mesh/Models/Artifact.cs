namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Artifact produced by a capability (source, binary, config).
/// </summary>
public record Artifact
{
    public required ArtifactFormat Format { get; init; }
    public required byte[] Content { get; init; }
    public string? ContentType { get; init; }
}

/// <summary>
/// Format of the artifact.
/// </summary>
public enum ArtifactFormat
{
    Source,
    Binary,
    Config,
}
