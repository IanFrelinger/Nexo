namespace Ashlar.Contracts.Distributed;

/// <summary>
/// Port for executing a <see cref="NativeArtifactManifest"/>. Implementations
/// live in the native product (WASM / out-of-process workers). The kernel must
/// never <c>dlopen</c> generated native code into the host process.
/// </summary>
public interface INativeExecutionHost
{
    /// <summary>
    /// Returns whether this host can run <paramref name="format"/>.
    /// </summary>
    /// <param name="format">Requested artifact format.</param>
    bool Supports(NativeArtifactFormat format);

    /// <summary>
    /// Binds <paramref name="manifest"/> and executes it under <paramref name="envelope"/> policy.
    /// </summary>
    /// <param name="manifest">Content-addressed artifact description.</param>
    /// <param name="envelope">Issuing envelope whose capabilities and policy apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ResultEvidence> ExecuteAsync(
        NativeArtifactManifest manifest,
        ExecutionEnvelope envelope,
        CancellationToken cancellationToken = default);
}
