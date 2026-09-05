namespace Ashlar.Contracts.Distributed;

/// <summary>
/// How a native or portable artifact is hosted. In-process <c>dlopen</c> of
/// generated native code is intentionally not a format: products must use
/// WebAssembly or an out-of-process worker.
/// </summary>
public enum NativeArtifactFormat
{
    /// <summary>WebAssembly module executed by a sandboxed host.</summary>
    WebAssembly = 0,

    /// <summary>Separate OS process (for example a Rust worker) reached over a local protocol.</summary>
    OutOfProcessWorker = 1,

    /// <summary>Managed assembly loaded in an isolated <c>AssemblyLoadContext</c>.</summary>
    ManagedAssembly = 2
}

/// <summary>
/// Content-addressed description of a portable artifact a native host may run.
/// </summary>
/// <param name="ArtifactId">Stable artifact identity (not the content digest).</param>
/// <param name="Format">Allowed host format. Never in-process generated native code.</param>
/// <param name="ContentHash">Digest of the artifact bytes the host must bind before execute.</param>
/// <param name="EntryPoint">Export, type, or process entry the host invokes.</param>
/// <param name="AllowedCapabilities">Capability names the artifact may request. Empty means none.</param>
public sealed record NativeArtifactManifest(
    string ArtifactId,
    NativeArtifactFormat Format,
    string ContentHash,
    string EntryPoint,
    IReadOnlyList<string>? AllowedCapabilities = null)
{
    /// <summary>
    /// Builds a manifest after rejecting blank required fields and undefined formats.
    /// </summary>
    public static NativeArtifactManifest Create(
        string artifactId,
        NativeArtifactFormat format,
        string contentHash,
        string entryPoint,
        IReadOnlyList<string>? allowedCapabilities = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPoint);
        DistributedContractGuard.Defined(format, nameof(format));

        return new NativeArtifactManifest(
            artifactId.Trim(),
            format,
            DistributedContractGuard.Digest(contentHash, nameof(contentHash)),
            entryPoint.Trim(),
            DistributedContractGuard.Capabilities(allowedCapabilities));
    }
}
