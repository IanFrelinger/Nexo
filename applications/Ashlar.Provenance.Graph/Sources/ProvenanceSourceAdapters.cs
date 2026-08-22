using Ashlar.Provenance.Graph.Ingestion;
using Ashlar.Provenance.Graph.Loading;
using Ashlar.Provenance.Graph.Models;
using Ashlar.Provenance.Graph.Ports;
using Ashlar.Provenance.Graph.Verification;

namespace Ashlar.Provenance.Graph.Sources;

/// <summary>Read-only adapter from authoritative certificate artifacts to projector input.</summary>
public interface IProvenanceSourceAdapter
{
    Task<IReadOnlyList<ProvenanceCertificateBundle>> ReadAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>Reads physical-atom certificate bundles from a directory tree.</summary>
public sealed class PhysicalAtomDirectorySourceAdapter : IProvenanceSourceAdapter
{
    private readonly string _rootDirectory;

    public PhysicalAtomDirectorySourceAdapter(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Certificate artifact directory is required.", nameof(rootDirectory));
        _rootDirectory = rootDirectory;
    }

    public Task<IReadOnlyList<ProvenanceCertificateBundle>> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ProvenanceCertificateBundle> bundles = PhysicalAtomBundleLoader
            .FindBundleFiles(_rootDirectory)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => PhysicalAtomBundleLoader.LoadFromJsonFile(path))
            .ToList();
        return Task.FromResult(bundles);
    }
}

/// <summary>
/// Computes the current head directly from verified source artifacts, never from Neo4j metadata.
/// </summary>
public sealed class SourceProvenanceChainHeadAuthority : IProvenanceChainHeadAuthority
{
    private readonly IProvenanceSourceAdapter _source;

    public SourceProvenanceChainHeadAuthority(IProvenanceSourceAdapter source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public async Task<string> GetCurrentChainHeadHashAsync(
        CancellationToken cancellationToken = default)
    {
        var bundles = await _source.ReadAsync(cancellationToken).ConfigureAwait(false);
        var records = new List<VerifiedProvenanceRecord>(bundles.Count);

        foreach (var bundle in bundles)
        {
            var verification = ProvenanceCertificateVerifier.Verify(bundle);
            if (!verification.Trusted)
            {
                throw new InvalidOperationException(
                    $"Authoritative certificate source contains invalid cert '{verification.CertificateHash}': " +
                    $"{verification.FailureCode} — {verification.Reason}");
            }

            records.Add(verification.Record!);
        }

        var certificateHashes = records.Select(record => record.CertificateHash).ToHashSet(StringComparer.Ordinal);
        var artifactIds = records.Select(record => record.ArtifactId).ToHashSet(StringComparer.Ordinal);
        if (records.Any(record =>
                record.PriorCertificateHash is not null
                && !certificateHashes.Contains(record.PriorCertificateHash)))
        {
            throw new InvalidOperationException("Authoritative certificate source has a missing chain reference.");
        }

        if (records.Any(record => record.DependsOnArtifactIds.Any(id => !artifactIds.Contains(id))))
            throw new InvalidOperationException("Authoritative certificate source has a missing dependency reference.");

        return ChainHeadCalculator.Compute(records);
    }
}
