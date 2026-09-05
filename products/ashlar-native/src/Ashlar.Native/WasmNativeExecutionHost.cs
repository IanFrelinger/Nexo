using Ashlar.Contracts.Distributed;

namespace Ashlar.Native;

/// <summary>
/// Scaffold native host. Accepts WebAssembly and out-of-process workers.
/// Refuses managed-assembly execution here (that stays in the kernel load
/// context) and never offers an in-process native/<c>dlopen</c> path.
/// </summary>
public sealed class WasmNativeExecutionHost : INativeExecutionHost
{
    /// <inheritdoc />
    public bool Supports(NativeArtifactFormat format) =>
        format is NativeArtifactFormat.WebAssembly or NativeArtifactFormat.OutOfProcessWorker;

    /// <inheritdoc />
    public Task<ResultEvidence> ExecuteAsync(
        NativeArtifactManifest manifest,
        ExecutionEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Supports(manifest.Format))
        {
            return Task.FromResult(ResultEvidence.Create(
                envelope.EnvelopeId,
                $"native:{manifest.ArtifactId}",
                ResultEvidenceStatus.Rejected,
                outputHash: string.Empty,
                DateTimeOffset.UtcNow,
                detail: $"Native host does not execute {manifest.Format}. Use WebAssembly or an out-of-process worker."));
        }

        if (!string.Equals(manifest.ContentHash, envelope.PayloadHash, StringComparison.Ordinal))
        {
            return Task.FromResult(ResultEvidence.Create(
                envelope.EnvelopeId,
                $"native:{manifest.ArtifactId}",
                ResultEvidenceStatus.Rejected,
                outputHash: string.Empty,
                DateTimeOffset.UtcNow,
                detail: "Envelope payload hash does not match artifact content hash."));
        }

        return Task.FromResult(ResultEvidence.Create(
            envelope.EnvelopeId,
            $"native:{manifest.ArtifactId}",
            ResultEvidenceStatus.Succeeded,
            manifest.ContentHash,
            DateTimeOffset.UtcNow));
    }
}
