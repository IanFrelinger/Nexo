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
    /// <summary>Source code (e.g. .cs files). Preferred when target CanCompile.</summary>
    Source = 0,
    /// <summary>Compiled binary (e.g. .dll).</summary>
    Binary = 1,
    /// <summary>Configuration only.</summary>
    Config = 2,
    /// <summary>Docker image. Preferred when target HasDockerRuntime.</summary>
    DockerImage = 3,
    /// <summary>WebAssembly module. Preferred when target HasWasmRuntime.</summary>
    WasmModule = 4,
}
